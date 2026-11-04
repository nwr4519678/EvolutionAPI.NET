namespace EvolutionApi.Domain.Entities;

/// <summary>
/// Stores an individual Signal Protocol key for one WhatsApp instance.
/// Used by SqlSignalKeyStore (implementing Baileys.NET ISignalKeyStore).
///
/// Categories: preKey, session, senderKey, senderKeyMemory, identity, etc.
/// KeyId is the string identifier used by Signal Protocol within a category.
/// </summary>
public sealed class SignalKey
{
    public long Id { get; set; }

    /// <summary>Foreign key — which WhatsApp instance owns this key.</summary>
    public string InstanceId { get; set; } = string.Empty;

    /// <summary>Signal Protocol category (preKey, session, senderKey, identity, etc.).</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Key identifier within the category (e.g. "0", "1234", or a JID).</summary>
    public string KeyId { get; set; } = string.Empty;

    /// <summary>Raw key bytes.</summary>
    public byte[] KeyData { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Instance? Instance { get; set; }
}
