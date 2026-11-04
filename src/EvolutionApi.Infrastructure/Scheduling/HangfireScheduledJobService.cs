using EvolutionApi.Domain.Entities;
using EvolutionApi.Domain.Interfaces;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EvolutionApi.Infrastructure.Scheduling;

/// <summary>
/// Hangfire implementation of IScheduledJobService.
/// Manages durable out-of-process scheduled background jobs for delayed webhook retries and scheduled broadcasts.
/// </summary>
public sealed class HangfireScheduledJobService(
    IBackgroundJobClient backgroundJobs,
    IServiceScopeFactory scopeFactory,
    ILogger<HangfireScheduledJobService> logger) : IScheduledJobService
{
    public string ScheduleWebhookRetry(string deliveryId, TimeSpan delay)
    {
        var jobId = backgroundJobs.Schedule<WebhookRetryJobExecutor>(
            x => x.ExecuteRetryAsync(deliveryId, CancellationToken.None),
            delay);

        logger.LogInformation("Scheduled Hangfire retry for webhook delivery {DeliveryId} with job ID {JobId} in {Delay}",
            deliveryId, jobId, delay);

        return jobId;
    }

    public string ScheduleBroadcastMessage(
        string tenantId,
        string instanceId,
        string recipient,
        string operation,
        string payloadJson,
        DateTime scheduledAtUtc)
    {
        using var scope = scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var job = new ScheduledMessageJob
        {
            TenantId = tenantId,
            InstanceId = instanceId,
            Recipient = recipient,
            Operation = operation,
            PayloadJson = payloadJson,
            ScheduledAtUtc = scheduledAtUtc,
            Status = "Scheduled"
        };

        uow.ScheduledMessages.AddAsync(job).GetAwaiter().GetResult();
        uow.SaveChangesAsync().GetAwaiter().GetResult();

        var delay = scheduledAtUtc > DateTime.UtcNow ? scheduledAtUtc - DateTime.UtcNow : TimeSpan.Zero;

        var hangfireJobId = backgroundJobs.Schedule<ScheduledMessageJobExecutor>(
            x => x.ExecuteScheduledJobAsync(job.Id, CancellationToken.None),
            delay);

        job.HangfireJobId = hangfireJobId;
        uow.ScheduledMessages.Update(job);
        uow.SaveChangesAsync().GetAwaiter().GetResult();

        logger.LogInformation("Scheduled broadcast message job {JobId} (Hangfire ID: {HangfireJobId}) for {ScheduledAt}",
            job.Id, hangfireJobId, scheduledAtUtc);

        return job.Id;
    }

    public bool CancelJob(string jobId)
    {
        try
        {
            return backgroundJobs.Delete(jobId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to cancel Hangfire job {JobId}", jobId);
            return false;
        }
    }
}

public sealed class WebhookRetryJobExecutor(
    IServiceScopeFactory scopeFactory,
    ILogger<WebhookRetryJobExecutor> logger)
{
    public async Task ExecuteRetryAsync(string deliveryId, CancellationToken ct)
    {
        logger.LogInformation("Executing delayed webhook delivery retry for {DeliveryId}", deliveryId);
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EvolutionApi.Infrastructure.Persistence.AppDbContext>();

        var delivery = await db.WebhookDeliveries.FindAsync([deliveryId], ct);
        if (delivery is not null && delivery.Status == "Retrying")
        {
            delivery.Status = "Pending";
            delivery.NextAttemptAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }
}
