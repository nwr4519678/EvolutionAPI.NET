using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;
using EvolutionApi.Domain.Entities;
using EvolutionApi.Domain.Interfaces;
using EvolutionApi.Infrastructure.WhatsApp.Engine;
using EvolutionApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace EvolutionApi.Infrastructure.WhatsApp.Services;

/// <summary>
/// In-process wake-up queue for durable SQL outbox rows. SQL Server remains the source
/// of truth; a process restart only loses these wake-up hints, not accepted messages.
/// </summary>
public sealed class OutboundMessageQueue
{
    private readonly Channel<string> _channel = Channel.CreateBounded<string>(new BoundedChannelOptions(500)
    {
        FullMode = BoundedChannelFullMode.DropWrite,
        SingleReader = true,
        SingleWriter = false
    });
    private readonly ConcurrentDictionary<string, TaskCompletionSource<JsonElement>> _waiters = new();

    public async Task<JsonElement> WaitForResult(string id, CancellationToken cancellationToken)
    {
        var completion = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_waiters.TryAdd(id, completion)) throw new InvalidOperationException("An outbox waiter already exists for this message.");
        try { return await completion.Task.WaitAsync(cancellationToken); }
        finally
        {
            if (_waiters.TryGetValue(id, out var current) && ReferenceEquals(current, completion)) _waiters.TryRemove(id, out _);
        }
    }

    public void Notify(string id) => _channel.Writer.TryWrite(id);
    internal void AbandonWaiter(string id) => _waiters.TryRemove(id, out _);
    internal bool TryRead(out string? id) => _channel.Reader.TryRead(out id);
    internal ValueTask<bool> WaitToReadAsync(CancellationToken ct) => _channel.Reader.WaitToReadAsync(ct);

    internal void Complete(string id, JsonElement result)
    {
        if (_waiters.TryRemove(id, out var waiter)) waiter.TrySetResult(result);
    }

    internal void Fail(string id, Exception exception)
    {
        if (_waiters.TryRemove(id, out var waiter)) waiter.TrySetException(exception);
    }
}

public sealed class OutboundMessageWorker(
    OutboundMessageQueue queue,
    IServiceScopeFactory scopeFactory,
    IWhatsAppEngine engine,
    ILogger<OutboundMessageWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ProcessingLease = TimeSpan.FromMinutes(2);
    private const int MaxAttempts = 8;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            string? id = null;
            try
            {
                if (!queue.TryRead(out id)) id = await FindNextMessageAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Failed to query pending outbound messages from database. Retrying in 5s...");
                try { await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); }
                catch (OperationCanceledException) { break; }
                continue;
            }

            if (id is null)
            {
                try
                {
                    using var waitCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    var channelWait = queue.WaitToReadAsync(waitCts.Token).AsTask();
                    var timer = Task.Delay(PollInterval, stoppingToken);
                    if (await Task.WhenAny(channelWait, timer) == channelWait)
                        await channelWait;
                    else
                    {
                        waitCts.Cancel();
                        try { await channelWait; }
                        catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested) { }
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
                continue;
            }

            try
            {
                await ProcessMessageAsync(id, stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Error occurred processing outbound message {Id}", id);
            }
        }
    }


    private async Task<string?> FindNextMessageAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        return await db.OutboundMessages.AsNoTracking()
            .Where(x => (x.Status == "Pending" && x.NextAttemptAt <= now)
                        || (x.Status == "Processing" && x.LeaseUntil <= now))
            .OrderBy(x => x.CreatedAt)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(ct);
    }

    private async Task ProcessMessageAsync(string id, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        var claimed = await db.OutboundMessages
            .Where(x => x.Id == id && ((x.Status == "Pending" && x.NextAttemptAt <= now)
                                      || (x.Status == "Processing" && x.LeaseUntil <= now)))
            .ExecuteUpdateAsync(update => update
                .SetProperty(x => x.Status, "Processing")
                .SetProperty(x => x.LeaseUntil, now.Add(ProcessingLease))
                .SetProperty(x => x.AttemptCount, x => x.AttemptCount + 1), ct);
        if (claimed == 0) return;

        var row = await db.OutboundMessages.SingleAsync(x => x.Id == id, ct);
        try
        {
            var payload = JsonSerializer.Deserialize<JsonElement>(row.PayloadJson);
            var result = await engine.SendAsync(row.InstanceId, row.Operation, payload, ct);
            row.Status = "Completed";
            row.CompletedAt = DateTime.UtcNow;
            row.LeaseUntil = null;
            row.ResultJson = result.GetRawText();
            row.LastError = null;
            await db.SaveChangesAsync(ct);
            queue.Complete(id, result);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Leave the lease in place; another worker can reclaim the row after it expires.
        }
        catch (Exception exception)
        {
            row.LastError = exception.ToString();
            row.LeaseUntil = null;
            var permanentFailure = exception is ArgumentException or KeyNotFoundException or WhatsAppEngineUnavailableException;
            if (permanentFailure || row.AttemptCount >= MaxAttempts)
            {
                row.Status = "Failed";
                await db.SaveChangesAsync(CancellationToken.None);
                logger.LogError(exception, "Outbound message {MessageId} permanently failed after {Attempts} attempts", id, row.AttemptCount);
                queue.Fail(id, exception);
                return;
            }

            row.Status = "Pending";
            var delaySeconds = Math.Min(300, 1 << Math.Min(row.AttemptCount, 8));
            row.NextAttemptAt = DateTime.UtcNow.AddSeconds(delaySeconds);
            await db.SaveChangesAsync(CancellationToken.None);
            logger.LogWarning(exception, "Outbound message {MessageId} failed on attempt {Attempt}; retrying in {DelaySeconds}s", id, row.AttemptCount, delaySeconds);
        }
    }
}

public sealed class MessageService(AppDbContext db, OutboundMessageQueue queue)
{
    public async Task<JsonElement> SendAsync(string instanceName, string operation, JsonElement payload, CancellationToken ct)
    {
        var instanceId = await db.Instances.Where(x => x.Name == instanceName).Select(x => x.Id).SingleOrDefaultAsync(ct)
                         ?? throw new KeyNotFoundException($"The '{instanceName}' instance does not exist");

        var row = new OutboundMessage
        {
            InstanceId = instanceId,
            Operation = operation,
            PayloadJson = payload.GetRawText()
        };
        var resultTask = queue.WaitForResult(row.Id, ct);
        db.OutboundMessages.Add(row);
        try { await db.SaveChangesAsync(ct); }
        catch
        {
            queue.AbandonWaiter(row.Id);
            throw;
        }
        queue.Notify(row.Id);
        return await resultTask;
    }
}
