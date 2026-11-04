using System.Collections.Concurrent;

namespace EvolutionApi.Infrastructure.WhatsApp.Engine;

/// <summary>
/// Thread-safe temporary store for inbound decrypted message content.
///
/// Flow:
///   1. Raw socket frame arrives ("message" node with &lt;enc&gt; child).
///   2. WaRawFrameInterceptor decrypts it → stores here keyed by msgId.
///   3. BaileysNetWhatsAppEngine.OnMessagesUpsert fires → enriches the
///      webhook payload with the decrypted content from this store.
///   4. Entry is evicted after retrieval or after TTL (60 s) to prevent leaks.
/// </summary>
public sealed class WaInboundMessageStore
{
    private record Entry(string ContentJson, DateTime ExpiresAt);

    private readonly ConcurrentDictionary<string, Entry> _store = new(StringComparer.Ordinal);

    // ── Write ─────────────────────────────────────────────────────────────────

    /// <summary>Stores decrypted message content for later retrieval.</summary>
    public void Store(string msgId, string contentJson) =>
        _store[msgId] = new Entry(contentJson, DateTime.UtcNow.AddSeconds(60));

    // ── Read ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Retrieves and removes decrypted content for a message ID.
    /// Returns null if not found or expired.
    /// </summary>
    public string? TakeContent(string msgId)
    {
        if (!_store.TryRemove(msgId, out var entry)) return null;
        if (entry.ExpiresAt < DateTime.UtcNow) return null;
        return entry.ContentJson;
    }

    // ── Housekeeping ─────────────────────────────────────────────────────────

    /// <summary>Prunes expired entries. Called periodically by the background cleaner.</summary>
    public void Prune()
    {
        var now = DateTime.UtcNow;
        foreach (var key in _store.Keys.ToArray())
            if (_store.TryGetValue(key, out var e) && e.ExpiresAt < now)
                _store.TryRemove(key, out _);
    }
}
