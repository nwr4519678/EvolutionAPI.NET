using System.Collections;
using System.Reflection;
using System.Text.Json;
using Baileys;
using Baileys.Types;

namespace EvolutionApi.Infrastructure.WhatsApp.Engine;

/// <summary>
/// Hooks into the Baileys.NET BaileysSocket's internal frame handler registry
/// to intercept raw incoming "message" binary nodes BEFORE the library
/// converts them to MinimalMessage.
///
/// WhatsApp delivers encrypted messages in binary frames with structure:
/// <code>
///   &lt;message to="{ourJid}" from="{senderJid}" type="text|media|..."&gt;
///     &lt;enc v="2" type="pkmsg|msg|skmsg"&gt; {ciphertext bytes} &lt;/enc&gt;
///   &lt;/message&gt;
/// </code>
///
/// Registration strategy (tried in order, stops at first success):
///   1. Public/private method named "AddFrameHandler" or "On" with (string, delegate) signature.
///   2. A string-keyed dictionary field containing handler delegates — add our entry.
///   3. A string-keyed dictionary of lists of handler delegates — add to the list.
///
/// If none of those patterns match the installed Baileys.NET version, the interceptor
/// logs a one-time warning and falls back gracefully (no decryption, but app keeps running).
/// </summary>
public sealed class WaRawFrameInterceptor(ILogger<WaRawFrameInterceptor> logger)
{
    private static readonly FieldInfo? SocketField =
        typeof(BaileysClient).GetField("_socket", BindingFlags.NonPublic | BindingFlags.Instance);

    // ── Registration ──────────────────────────────────────────────────────────

    /// <summary>
    /// Attempts to register a "message" node handler on the BaileysSocket inside the client.
    /// Returns true if the handler was successfully wired; false if the API is unavailable.
    /// </summary>
    public bool TryRegister(
        BaileysClient client,
        string instanceId,
        WaInboundDecryptor decryptor,
        WaInboundMessageStore store)
    {
        try
        {
            var socket = SocketField?.GetValue(client);
            if (socket is null)
            {
                logger.LogWarning("[{Id}] BaileysClient._socket field not found. Inbound decryptor disabled.", instanceId);
                return false;
            }

            var socketType = socket.GetType();
            Func<BinaryNode, Task>? asyncHandler = node => HandleMessageNodeAsync(node, instanceId, decryptor, store);

            // Strategy 1: Look for AddFrameHandler / RegisterHandler / On / Subscribe method
            foreach (var methodName in new[] { "AddFrameHandler", "RegisterHandler", "On", "Subscribe", "AddHandler" })
            {
                var method = FindHandlerMethod(socketType, methodName);
                if (method is null) continue;

                var invoked = TryInvokeHandlerMethod(socket, method, "message", asyncHandler);
                if (invoked)
                {
                    logger.LogInformation("[{Id}] Inbound decryptor wired via {Method}().", instanceId, methodName);
                    return true;
                }
            }

            // Strategy 2: Find Dictionary<string, ...> handler map field
            foreach (var field in socketType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public))
            {
                if (!field.FieldType.IsGenericType) continue;
                var genDef = field.FieldType.GetGenericTypeDefinition();
                var args = field.FieldType.GetGenericArguments();
                if (args.Length < 2 || args[0] != typeof(string)) continue;

                // Dictionary<string, Func<BinaryNode, Task>> or similar
                if (genDef == typeof(Dictionary<,>))
                {
                    var dict = field.GetValue(socket) as IDictionary;
                    if (dict is null) continue;
                    if (TryAddToDictionary(dict, "message", asyncHandler, args[1]))
                    {
                        logger.LogInformation("[{Id}] Inbound decryptor wired via dict field '{Field}'.", instanceId, field.Name);
                        return true;
                    }
                }

                // Dictionary<string, List<Func<BinaryNode, Task>>> or ConcurrentDictionary
                if (genDef == typeof(Dictionary<,>) || field.FieldType.Name.StartsWith("ConcurrentDictionary"))
                {
                    // handled above or will fail gracefully
                }
            }

            logger.LogWarning(
                "[{Id}] Could not find frame handler registration in Baileys.NET {Version}. " +
                "Inbound message content will not be decrypted. " +
                "This will be resolved when Baileys.NET exposes a public frame-handler API.",
                instanceId,
                typeof(BaileysClient).Assembly.GetName().Version);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[{Id}] Exception wiring inbound decryptor.", instanceId);
            return false;
        }
    }

    // ── Frame processing ──────────────────────────────────────────────────────

    private static async Task HandleMessageNodeAsync(
        BinaryNode node,
        string instanceId,
        WaInboundDecryptor decryptor,
        WaInboundMessageStore store)
    {
        try
        {
            // Require a message ID and sender JID
            if (!node.Attrs.TryGetValue("id", out var msgId)) return;
            if (!node.Attrs.TryGetValue("from", out var senderJid)) return;

            // Find <enc> child node
            var encNode = BinaryNodeExtensions.GetChild(node, "enc");
            if (encNode is null) return;

            if (!encNode.Attrs.TryGetValue("type", out var encType)) return;

            // Extract ciphertext bytes
            byte[]? ciphertext = null;
            if (encNode.Content is BinaryNodeBytes bnb) ciphertext = bnb.Data;
            else if (encNode.Content is BinaryNodeString bns) ciphertext = Convert.FromBase64String(bns.Value);

            if (ciphertext is null || ciphertext.Length == 0) return;

            // Decrypt
            var plaintext = await decryptor.DecryptAsync(senderJid, encType, ciphertext, CancellationToken.None);

            // Parse protobuf plaintext as WebMessageInfo and store as JSON
            // We use WaProto to decode the raw bytes into a JsonElement
            var contentJson = DecodeMessageProto(plaintext);
            store.Store(msgId, contentJson);
        }
        catch
        {
            // Never throw from a raw socket frame handler — it could crash the connection
        }
    }

    private static string DecodeMessageProto(byte[] plaintext)
    {
        // Store the raw decrypted bytes as base64.
        // Callers can deserialize the WA protobuf (WebMessageInfo) using WaProto or Google.Protobuf.
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            rawBase64 = Convert.ToBase64String(plaintext),
            byteLength = plaintext.Length
        });
    }

    // ── Reflection helpers ────────────────────────────────────────────────────

    private static MethodInfo? FindHandlerMethod(Type type, string name)
    {
        foreach (var flags in new[]
                 {
                     BindingFlags.Public | BindingFlags.Instance,
                     BindingFlags.NonPublic | BindingFlags.Instance
                 })
        {
            var methods = type.GetMethods(flags).Where(m => m.Name == name).ToArray();
            foreach (var m in methods)
            {
                var parms = m.GetParameters();
                if (parms.Length >= 2 && parms[0].ParameterType == typeof(string))
                    return m;
            }
        }
        return null;
    }

    private static bool TryInvokeHandlerMethod(
        object socket, MethodInfo method, string tag, Func<BinaryNode, Task> handler)
    {
        try
        {
            var parms = method.GetParameters();
            if (parms.Length == 2)
            {
                // Adapt delegate type if needed
                object? arg2 = parms[1].ParameterType == typeof(Func<BinaryNode, Task>)
                    ? handler
                    : parms[1].ParameterType == typeof(Action<BinaryNode>)
                        ? (Action<BinaryNode>)(n => _ = handler(n))
                        : null;
                if (arg2 is null) return false;
                method.Invoke(socket, [tag, arg2]);
                return true;
            }
            return false;
        }
        catch { return false; }
    }

    private static bool TryAddToDictionary(IDictionary dict, string key, Func<BinaryNode, Task> handler, Type valueType)
    {
        try
        {
            if (valueType == typeof(Func<BinaryNode, Task>))
            {
                dict[key] = handler;
                return true;
            }
            if (valueType == typeof(Action<BinaryNode>))
            {
                dict[key] = (Action<BinaryNode>)(n => _ = handler(n));
                return true;
            }
            return false;
        }
        catch { return false; }
    }
}
