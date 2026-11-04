using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Baileys;
using Baileys.Socket;
using Baileys.Types;
using Baileys.Utils;
using Baileys.WABinary;

namespace EvolutionApi.Infrastructure.WhatsApp.Engine;

/// <summary>
/// Provides reflection-based access to the private <see cref="BaileysSocket"/> inside
/// <see cref="BaileysClient"/> and exposes all WhatsApp IQ/binary-node operations.
/// Note: The _socket field is private in Baileys.NET 1.9.1. Once the package exposes it
/// publicly this class can be simplified to a direct property access.
/// </summary>
public sealed class WaSocket
{
    private static readonly FieldInfo SocketField =
        typeof(BaileysClient).GetField("_socket", BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new InvalidOperationException("BaileysClient._socket field not found. Baileys.NET version may have changed.");

    private readonly BaileysSocket _socket;

    public WaSocket(BaileysClient client)
    {
        _socket = (BaileysSocket?)SocketField.GetValue(client)
                  ?? throw new InvalidOperationException("BaileysClient._socket is null — the client is not connected.");
    }

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    // ── Core ───────────────────────────────────────────────────────────────────

    public Task SendNodeAsync(BinaryNode node, CancellationToken ct = default) =>
        _socket.SendNodeAsync(node, ct);

    public Task<BinaryNode> QueryAsync(BinaryNode node, CancellationToken ct = default) =>
        _socket.QueryAsync(node, ct);

    // BinaryNodeExtensions static methods — dot-notation doesn't compile for this package
    // (extension class not decorated as C# extension), so call via static helpers.
    private static BinaryNode? Child(BinaryNode node, string tag) => BinaryNodeExtensions.GetChild(node, tag);
    private static IEnumerable<BinaryNode> Children(BinaryNode node) => BinaryNodeExtensions.GetChildren(node);
    private static string? Attr(BinaryNode node, string key) => node.Attrs.TryGetValue(key, out var v) ? v : null;

    // IQ id: short unique tag; message id: Baileys standard format
    private static string IqId()      => Generics.GenerateMdTagPrefix();
    private static string MessageId() => Generics.GenerateMessageId();

    private static BinaryNode Iq(string type, string xmlns, string to, BinaryNode[] children) =>
        new()
        {
            Tag = "iq",
            Attrs = new Dictionary<string, string>
            {
                ["id"] = IqId(), ["type"] = type, ["xmlns"] = xmlns, ["to"] = to
            },
            Content = new BinaryNodeList(children)
        };

    private static BinaryNode Node(string tag, Dictionary<string, string>? attrs = null, BinaryNode[]? children = null, byte[]? data = null)
    {
        BinaryNodeContent? content = children is { Length: > 0 }
            ? new BinaryNodeList(children)
            : data is not null
                ? new BinaryNodeBytes(data)
                : null;
        return new BinaryNode { Tag = tag, Attrs = attrs ?? [], Content = content };
    }

    // ── Presence ──────────────────────────────────────────────────────────────

    /// <summary>Sets global connection presence (available / unavailable).</summary>
    public Task SetPresenceAsync(string presenceType, CancellationToken ct) =>
        SendNodeAsync(Node("presence", new() { ["type"] = presenceType }), ct);

    /// <summary>Sends composing/recording/paused chat-state to a specific JID.</summary>
    public Task SendChatStateAsync(string toJid, string state, CancellationToken ct) =>
        SendNodeAsync(Node("chatstate", new() { ["to"] = toJid }, [Node(state)]), ct);

    // ── Profile picture ───────────────────────────────────────────────────────

    /// <summary>Fetches the profile picture URL for a JID. Returns null when none is set.</summary>
    public async Task<string?> FetchProfilePictureUrlAsync(string jid, CancellationToken ct)
    {
        try
        {
            var result = await QueryAsync(Iq("get", "w:profile:picture", jid,
                [Node("picture", new() { ["type"] = "image", ["query"] = "url" })]), ct);
            return Child(result, "picture") is { } pic ? Attr(pic, "url") : null;
        }
        catch { return null; }
    }

    /// <summary>
    /// Updates the account or group profile picture.
    /// Sends the raw image bytes (JPEG, max 640×640) via a w:profile:picture IQ.
    /// </summary>
    public async Task<JsonElement> UpdateProfilePictureAsync(string jid, byte[] jpegBytes, CancellationToken ct)
    {
        var result = await QueryAsync(new BinaryNode
        {
            Tag = "iq",
            Attrs = new Dictionary<string, string>
            {
                ["id"] = IqId(), ["to"] = jid, ["type"] = "set", ["xmlns"] = "w:profile:picture"
            },
            Content = new BinaryNodeList([
                new BinaryNode
                {
                    Tag = "picture",
                    Attrs = new Dictionary<string, string> { ["type"] = "image" },
                    Content = new BinaryNodeBytes(jpegBytes)
                }
            ])
        }, ct);
        var eurl = Child(result, "picture") is { } p ? Attr(p, "eurl") : null;
        return JsonSerializer.SerializeToElement(new { jid, updated = true, eurl }, JsonOpts);
    }

    /// <summary>Removes the profile picture for the account.</summary>
    public async Task<JsonElement> RemoveProfilePictureAsync(string jid, CancellationToken ct)
    {
        await QueryAsync(new BinaryNode
        {
            Tag = "iq",
            Attrs = new Dictionary<string, string>
            {
                ["id"] = IqId(), ["to"] = jid, ["type"] = "set", ["xmlns"] = "w:profile:picture"
            },
            Content = new BinaryNodeList([
                new BinaryNode
                {
                    Tag = "picture",
                    Attrs = new Dictionary<string, string> { ["type"] = "delete" },
                    Content = null
                }
            ])
        }, ct);
        return JsonSerializer.SerializeToElement(new { jid, removed = true }, JsonOpts);
    }

    /// <summary>
    /// Fetches the WhatsApp Business profile for a JID.
    /// Requires the connected account to be a WA Business account.
    /// </summary>
    public async Task<JsonElement> FetchBusinessProfileAsync(string jid, CancellationToken ct)
    {
        try
        {
            var result = await QueryAsync(new BinaryNode
            {
                Tag = "iq",
                Attrs = new Dictionary<string, string>
                {
                    ["id"] = IqId(), ["to"] = "s.whatsapp.net", ["type"] = "get", ["xmlns"] = "w:biz"
                },
                Content = new BinaryNodeList([
                    new BinaryNode
                    {
                        Tag = "business_profile",
                        Attrs = new Dictionary<string, string> { ["v"] = "116" },
                        Content = new BinaryNodeList([
                            new BinaryNode
                            {
                                Tag = "profile",
                                Attrs = new Dictionary<string, string> { ["jid"] = jid },
                                Content = null
                            }
                        ])
                    }
                ])
            }, ct);

            var profile = Child(result, "business_profile") is { } bp ? Child(bp, "profile") : null;
            return JsonSerializer.SerializeToElement(new
            {
                jid,
                description = profile is null ? null : Attr(profile, "description"),
                address = profile is null ? null : Attr(profile, "address"),
                category = profile is null ? null : Attr(profile, "category"),
                email = profile is null ? null : Attr(profile, "email"),
                website = profile is null ? null : Attr(profile, "website"),
                profilePictureUrl = profile is null ? null : Attr(profile, "profile_picture_url")
            }, JsonOpts);
        }
        catch
        {
            return JsonSerializer.SerializeToElement(new
            {
                jid,
                description = (string?)null,
                address = (string?)null,
                category = (string?)null,
                email = (string?)null,
                website = (string?)null,
                profilePictureUrl = (string?)null,
                error = "Business profile not available (ensure a WA Business account is connected)"
            }, JsonOpts);
        }
    }

    /// <summary>Fetches the status/about text for a JID.</summary>
    public async Task<string?> FetchProfileStatusAsync(string jid, CancellationToken ct)
    {
        try
        {
            var result = await QueryAsync(Iq("get", "status", "s.whatsapp.net",
                [Node("user", new() { ["jid"] = jid })]), ct);
            var user = Child(result, "user");
            return user is null ? null : Attr(user, "status");
        }
        catch { return null; }
    }

    // ── Profile name / status ─────────────────────────────────────────────────

    /// <summary>Updates the account display name.</summary>
    public Task UpdateProfileNameAsync(string name, CancellationToken ct) =>
        QueryAsync(Iq("set", "vcard", "s.whatsapp.net",
            [Node("vcard", data: Encoding.UTF8.GetBytes($"BEGIN:VCARD\nVERSION:3.0\nFN:{name}\nEND:VCARD"))]), ct);

    /// <summary>Updates the account status (About) text.</summary>
    public Task UpdateProfileStatusAsync(string status, CancellationToken ct) =>
        QueryAsync(Iq("set", "status", "s.whatsapp.net",
            [Node("status", data: Encoding.UTF8.GetBytes(status))]), ct);

    // ── Block list ────────────────────────────────────────────────────────────

    /// <summary>Blocks or unblocks a JID. action = "block" | "unblock".</summary>
    public Task UpdateBlockStatusAsync(string jid, string action, CancellationToken ct) =>
        QueryAsync(Iq("set", "blocklist", "s.whatsapp.net",
            [Node("item", new() { ["jid"] = jid, ["action"] = action })]), ct);

    // ── Mark messages as read ─────────────────────────────────────────────────

    /// <summary>Sends a read receipt for one or more message IDs in a chat.</summary>
    public Task MarkAsReadAsync(string jid, string[] messageIds, string? participant, CancellationToken ct)
    {
        var attrs = new Dictionary<string, string>
        {
            ["to"] = jid, ["type"] = "chat", ["count"] = messageIds.Length.ToString()
        };
        if (participant is not null) attrs["participant"] = participant;
        var items = messageIds.Select(id => Node("item", new() { ["id"] = id })).ToArray();
        return SendNodeAsync(Node("read", attrs, items), ct);
    }

    // ── Delete message for everyone ───────────────────────────────────────────

    /// <summary>Sends a revoke (delete-for-everyone) node for the specified message.</summary>
    public Task DeleteMessageForEveryoneAsync(string jid, string msgId, bool fromMe, string? participant, CancellationToken ct)
    {
        var keyAttrs = new Dictionary<string, string>
        {
            ["id"] = msgId, ["remoteJid"] = jid, ["fromMe"] = fromMe ? "true" : "false"
        };
        if (participant is not null) keyAttrs["participant"] = participant;
        return SendNodeAsync(Node("message", new() { ["to"] = jid, ["type"] = "revoke", ["id"] = MessageId() },
            [Node("revoke", keyAttrs)]), ct);
    }

    // ── Privacy settings ─────────────────────────────────────────────────────

    public async Task<JsonElement> FetchPrivacySettingsAsync(CancellationToken ct)
    {
        var result = await QueryAsync(Iq("get", "privacy", "s.whatsapp.net", [Node("privacy")]), ct);
        var dict = new Dictionary<string, string>();
        var privacy = Child(result, "privacy");
        if (privacy is not null)
            foreach (var cat in Children(privacy))
                if (cat.Attrs.TryGetValue("name", out var name) && cat.Attrs.TryGetValue("value", out var val))
                    dict[name] = val;
        return JsonSerializer.SerializeToElement(dict);
    }

    public Task UpdatePrivacySettingsAsync(Dictionary<string, string> categories, CancellationToken ct)
    {
        var cats = categories.Select(kvp =>
            Node("category", new() { ["name"] = kvp.Key, ["value"] = kvp.Value })).ToArray();
        return QueryAsync(Iq("set", "privacy", "s.whatsapp.net", [Node("privacy", children: cats)]), ct);
    }

    // ── Check WhatsApp numbers ────────────────────────────────────────────────

    /// <summary>Checks whether phone numbers are registered on WhatsApp. Returns JID info per number.</summary>
    public async Task<JsonElement> CheckWhatsAppNumbersAsync(string[] numbers, CancellationToken ct)
    {
        var contacts = numbers.Select(n =>
            Node("contact", new() { ["jid"] = NormalizeJid(n) })).ToArray();
        var result = await QueryAsync(Iq("get", "usync", "s.whatsapp.net",
            [Node("usync", new() { ["sid"] = IqId(), ["mode"] = "query", ["last"] = "true", ["index"] = "0", ["context"] = "interactive" },
                [Node("query", children: [Node("contact")]),
                 Node("list", children: contacts)])]), ct);

        var list = new List<object>();
        var usync = Child(result, "usync") is { } u ? Child(u, "list") : null;
        if (usync is not null)
            foreach (var user in Children(usync))
            {
                var jid = Attr(user, "jid");
                var contact = Child(user, "contact");
                var registered = (contact is null ? null : Attr(contact, "type")) == "in";
                list.Add(new { jid, number = JidToNumber(jid), exists = registered });
            }
        return JsonSerializer.SerializeToElement(list);
    }

    // ── Groups ────────────────────────────────────────────────────────────────

    /// <summary>Creates a new group. Returns the new group JID and metadata.</summary>
    public async Task<JsonElement> CreateGroupAsync(string subject, string[] participantJids, CancellationToken ct)
    {
        var participants = participantJids.Select(j => Node("participant", new() { ["jid"] = j })).ToArray();
        var result = await QueryAsync(Iq("set", "w:g2", "@g.us",
            [Node("create", new() { ["subject"] = subject, ["key"] = Generics.GenerateMdTagPrefix() },
                participants)]), ct);

        var group = Child(result, "group");
        return JsonSerializer.SerializeToElement(new
        {
            id = (group is null ? null : Attr(group, "id")),
            subject,
            participants = participantJids
        });
    }

    /// <summary>Fetches metadata for a group JID.</summary>
    public async Task<JsonElement> FetchGroupMetadataAsync(string groupJid, CancellationToken ct)
    {
        var result = await QueryAsync(Iq("get", "w:g2", groupJid,
            [Node("query", new() { ["request"] = "interactive" })]), ct);
        return ParseGroupMetadata(Child(result, "group") ?? result);
    }

    /// <summary>Fetches all groups the account participates in.</summary>
    public async Task<JsonElement> FetchAllGroupsAsync(CancellationToken ct)
    {
        var result = await QueryAsync(Iq("get", "w:g2", "@g.us",
            [Node("participating", children:
                [Node("participants"), Node("description")])]), ct);
        var groups = new List<object>();
        var list = Child(result, "groups");
        if (list is not null)
            foreach (var g in Children(list))
                groups.Add(ParseGroupMetadataDynamic(g));
        return JsonSerializer.SerializeToElement(groups);
    }

    /// <summary>Adds, removes, promotes or demotes participants. action = add|remove|promote|demote.</summary>
    public async Task<JsonElement> UpdateParticipantsAsync(string groupJid, string[] participantJids, string action, CancellationToken ct)
    {
        var participants = participantJids.Select(j => Node("participant", new() { ["jid"] = j })).ToArray();
        var result = await QueryAsync(Iq("set", "w:g2", groupJid,
            [Node(action, children: participants)]), ct);
        var updates = new List<object>();
        var actionNode = BinaryNodeExtensions.GetChild(result, action);
        if (actionNode is not null)
            foreach (var p in Children(actionNode))
                updates.Add(new
                {
                    jid = Attr(p, "jid"),
                    status = (p.Attrs.TryGetValue("error", out var _v_error) ? _v_error : "200")
                });
        return JsonSerializer.SerializeToElement(updates);
    }

    /// <summary>Updates the group subject (name).</summary>
    public Task UpdateGroupSubjectAsync(string groupJid, string subject, CancellationToken ct) =>
        QueryAsync(Iq("set", "w:g2", groupJid,
            [Node("subject", data: Encoding.UTF8.GetBytes(subject))]), ct);

    /// <summary>Updates the group description.</summary>
    public Task UpdateGroupDescriptionAsync(string groupJid, string description, CancellationToken ct) =>
        QueryAsync(Iq("set", "w:g2", groupJid,
            [Node("description", new() { ["id"] = IqId() },
                [Node("body", data: Encoding.UTF8.GetBytes(description))])]), ct);

    /// <summary>Returns the current invite link for a group.</summary>
    public async Task<string?> FetchGroupInviteCodeAsync(string groupJid, CancellationToken ct)
    {
        var result = await QueryAsync(Iq("get", "w:g2", groupJid, [Node("invite")]), ct);
        return Child(result, "invite") is { } inv1 ? Attr(inv1, "code") : null;
    }

    /// <summary>Revokes and returns a new invite link for a group.</summary>
    public async Task<string?> RevokeGroupInviteCodeAsync(string groupJid, CancellationToken ct)
    {
        var result = await QueryAsync(Iq("set", "w:g2", groupJid, [Node("invite")]), ct);
        return Child(result, "invite") is { } inv2 ? Attr(inv2, "code") : null;
    }

    /// <summary>Toggles group settings: announcement (only admins post) or restrict (only admins edit info).</summary>
    public Task UpdateGroupSettingAsync(string groupJid, string setting, bool enable, CancellationToken ct) =>
        QueryAsync(Iq("set", "w:g2", groupJid,
            [Node(enable ? setting : $"not_{setting}")]), ct);

    /// <summary>Sets or clears the group ephemeral (disappearing messages) duration in seconds.</summary>
    public Task ToggleEphemeralAsync(string groupJid, int durationSeconds, CancellationToken ct) =>
        SendNodeAsync(Node("message",
            new() { ["to"] = groupJid, ["type"] = "text", ["id"] = MessageId() },
            [Node("ephemeral", new() { ["expiration"] = durationSeconds.ToString() })]), ct);

    /// <summary>Leaves a group.</summary>
    public Task LeaveGroupAsync(string groupJid, CancellationToken ct) =>
        QueryAsync(Iq("set", "w:g2", "@g.us",
            [Node("leave", children: [Node("group", new() { ["id"] = groupJid })])]), ct);

    /// <summary>Returns metadata for the group invite link.</summary>
    public async Task<JsonElement> FetchGroupInviteInfoAsync(string code, CancellationToken ct)
    {
        var result = await QueryAsync(Iq("get", "w:g2", "invite.whatsapp.net@g.us",
            [Node("invite", new() { ["code"] = code })]), ct);
        return ParseGroupMetadata(Child(result, "group") ?? result);
    }

    /// <summary>Joins a group via invite code.</summary>
    public async Task<JsonElement> AcceptGroupInviteAsync(string code, CancellationToken ct)
    {
        var result = await QueryAsync(Iq("set", "w:g2", "invite.whatsapp.net@g.us",
            [Node("invite", new() { ["code"] = code })]), ct);
        var group = Child(result, "group");
        return JsonSerializer.SerializeToElement(new { id = (group is null ? null : Attr(group, "jid")) });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static JsonElement ParseGroupMetadata(BinaryNode node)
    {
        var participants = Children(node)
            .Where(c => c.Tag == "participant")
            .Select(p => new
            {
                id = Attr(p, "jid"),
                admin = Attr(p, "type"),
                isAdmin = Attr(p, "type") == "admin",
                isSuperAdmin = Attr(p, "type") == "superadmin"
            }).ToArray();
        return JsonSerializer.SerializeToElement(new
        {
            id = Attr(node, "id"),
            subject = Attr(node, "subject"),
            subjectTime = Attr(node, "s_t"),
            creation = Attr(node, "creation"),
            owner = Attr(node, "creator"),
            participants
        });
    }

    private static object ParseGroupMetadataDynamic(BinaryNode node)
    {
        var participants = Children(node)
            .Where(c => c.Tag == "participant")
            .Select(p => new
            {
                id = Attr(p, "jid"),
                admin = Attr(p, "type")
            }).ToArray();
        return new
        {
            id = Attr(node, "id"),
            subject = Attr(node, "subject"),
            participants
        };
    }

    private static string NormalizeJid(string number)
    {
        var digits = new string(number.Where(char.IsDigit).ToArray());
        return JidUtils.JidEncode(digits, JidServer.SWhatsappNet);
    }

    /// <summary>Creates a new label on the WhatsApp account.</summary>
    public async Task<JsonElement> CreateLabelAsync(string name, string color, CancellationToken ct)
    {
        var labelId = (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 100000).ToString();
        var colorIndex = MapColorToIndex(color);
        var iq = new BinaryNode
        {
            Tag = "iq",
            Attrs = new() { ["to"] = "@s.whatsapp.net", ["type"] = "set", ["xmlns"] = "w:label:user" },
            Content = new BinaryNodeList([
                new BinaryNode
                {
                    Tag = "label",
                    Attrs = new()
                    {
                        ["id"] = labelId, ["name"] = name,
                        ["color"] = colorIndex.ToString(), ["deleted"] = "false"
                    }
                }
            ])
        };
        await _socket.QueryAsync(iq, ct);
        return JsonSerializer.SerializeToElement(new { labelId, name, color, colorIndex }, JsonOpts);
    }

    /// <summary>Updates an existing label's name and/or color.</summary>
    public async Task<JsonElement> UpdateLabelAsync(string labelId, string? name, string? color, CancellationToken ct)
    {
        var attrs = new Dictionary<string, string> { ["id"] = labelId, ["deleted"] = "false" };
        if (name is not null) attrs["name"] = name;
        if (color is not null) attrs["color"] = MapColorToIndex(color).ToString();

        var iq = new BinaryNode
        {
            Tag = "iq",
            Attrs = new() { ["to"] = "@s.whatsapp.net", ["type"] = "set", ["xmlns"] = "w:label:user" },
            Content = new BinaryNodeList([new BinaryNode { Tag = "label", Attrs = attrs }])
        };
        await _socket.QueryAsync(iq, ct);
        return JsonSerializer.SerializeToElement(new { labelId, name, color, updated = true }, JsonOpts);
    }

    /// <summary>Deletes a label from the WhatsApp account.</summary>
    public async Task<JsonElement> DeleteLabelAsync(string labelId, CancellationToken ct)
    {
        var iq = new BinaryNode
        {
            Tag = "iq",
            Attrs = new() { ["to"] = "@s.whatsapp.net", ["type"] = "set", ["xmlns"] = "w:label:user" },
            Content = new BinaryNodeList([
                new BinaryNode { Tag = "label", Attrs = new() { ["id"] = labelId, ["deleted"] = "true" } }
            ])
        };
        await _socket.QueryAsync(iq, ct);
        return JsonSerializer.SerializeToElement(new { labelId, deleted = true }, JsonOpts);
    }

    /// <summary>Maps an HTML hex color to a WA label color index (0–19).</summary>
    private static int MapColorToIndex(string color)
    {
        // WA supports 20 predefined colors (0–19); map common hex values
        return color?.ToUpperInvariant() switch
        {
            "#E53935" or "#F44336" => 0,   // red
            "#E91E63" or "#FF4081" => 1,   // pink
            "#9C27B0" or "#AA00FF" => 2,   // purple
            "#3F51B5" or "#283593" => 3,   // indigo
            "#2196F3" or "#1565C0" => 4,   // blue
            "#03A9F4" or "#0288D1" => 5,   // light blue
            "#00BCD4" or "#006064" => 6,   // cyan
            "#009688" or "#004D40" => 7,   // teal
            "#4CAF50" or "#1B5E20" => 8,   // green
            "#8BC34A" or "#33691E" => 9,   // light green
            "#CDDC39" or "#827717" => 10,  // lime
            "#FFEB3B" or "#F57F17" => 11,  // yellow
            "#FFC107" or "#FF6F00" => 12,  // amber
            "#FF9800" or "#E65100" => 13,  // orange
            "#FF5722" or "#BF360C" => 14,  // deep orange
            "#795548" or "#3E2723" => 15,  // brown
            "#9E9E9E" or "#424242" => 16,  // grey
            "#607D8B" or "#263238" => 17,  // blue grey
            _ => 4 // default: blue
        };
    }

    private static string? JidToNumber(string? jid) =>
        jid?.Split('@').FirstOrDefault();
}
