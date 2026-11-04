using System.Text.Json;
using EvolutionApi.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace EvolutionApi.Infrastructure.Scheduling;

/// <summary>
/// Hangfire job executor for out-of-process scheduled broadcast messages.
/// </summary>
public sealed class ScheduledMessageJobExecutor(
    IUnitOfWork uow,
    IThrottledMessageQueue throttler,
    ILogger<ScheduledMessageJobExecutor> logger)
{
    public async Task ExecuteScheduledJobAsync(string scheduledJobId, CancellationToken ct)
    {
        logger.LogInformation("Executing Hangfire scheduled broadcast job {JobId}", scheduledJobId);

        var job = await uow.ScheduledMessages.GetByIdAsync(scheduledJobId, ct);
        if (job is null)
        {
            logger.LogWarning("Scheduled job {JobId} not found in database", scheduledJobId);
            return;
        }

        if (job.Status is "Completed" or "Cancelled")
        {
            logger.LogInformation("Job {JobId} is already in {Status} state. Skipping", scheduledJobId, job.Status);
            return;
        }

        var instance = await uow.Instances.GetByIdAsync(job.InstanceId, ct);
        if (instance is null)
        {
            job.Status = "Failed";
            job.LastError = $"Instance {job.InstanceId} no longer exists";
            job.ExecutedAtUtc = DateTime.UtcNow;
            await uow.SaveChangesAsync(ct);
            return;
        }

        try
        {
            job.Status = "Executing";
            await uow.SaveChangesAsync(ct);

            var payload = JsonSerializer.Deserialize<JsonElement>(job.PayloadJson);

            await throttler.EnqueueAndAwaitAsync(
                job.TenantId ?? "default",
                instance.Id,
                instance.Name,
                job.Operation,
                payload,
                ct);

            job.Status = "Completed";
            job.ExecutedAtUtc = DateTime.UtcNow;
            job.LastError = null;
            await uow.SaveChangesAsync(ct);

            logger.LogInformation("Successfully completed Hangfire scheduled job {JobId}", scheduledJobId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to execute Hangfire scheduled job {JobId}", scheduledJobId);
            job.Status = "Failed";
            job.LastError = ex.Message;
            job.ExecutedAtUtc = DateTime.UtcNow;
            await uow.SaveChangesAsync(CancellationToken.None);
            throw; // Rethrow to let Hangfire trigger its retry policy
        }
    }
}
