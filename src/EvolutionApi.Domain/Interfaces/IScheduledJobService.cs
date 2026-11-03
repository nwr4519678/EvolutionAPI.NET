namespace EvolutionApi.Domain.Interfaces;

/// <summary>
/// Contract for out-of-process scheduled background jobs (Hangfire / Quartz).
/// Handles scheduled broadcast messages and delayed webhook retries.
/// </summary>
public interface IScheduledJobService
{
    string ScheduleWebhookRetry(string deliveryId, TimeSpan delay);

    string ScheduleBroadcastMessage(
        string tenantId,
        string instanceId,
        string recipient,
        string operation,
        string payloadJson,
        DateTime scheduledAtUtc);

    bool CancelJob(string jobId);
}
