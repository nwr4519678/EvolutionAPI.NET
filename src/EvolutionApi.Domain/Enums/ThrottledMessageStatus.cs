namespace EvolutionApi.Domain.Enums;

/// <summary>
/// Processing state for outbound messages passing through the per-tenant throttled channel queue.
/// </summary>
public enum ThrottledMessageStatus
{
    Pending = 0,
    Queued = 1,
    Processing = 2,
    Sent = 3,
    Delivered = 4,
    Failed = 5
}
