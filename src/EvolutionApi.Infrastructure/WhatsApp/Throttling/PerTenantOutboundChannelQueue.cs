using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;
using EvolutionApi.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace EvolutionApi.Infrastructure.WhatsApp.Throttling;

/// <summary>
/// In-memory, thread-safe, per-tenant throttling queue backed by System.Threading.Channels.
/// Enforces safe 3-6 second delays between outbound messages per tenant instance to prevent number banning.
/// </summary>
public sealed class PerTenantOutboundChannelQueue : IThrottledMessageQueue, IAsyncDisposable
{
    private readonly IWhatsAppEngine _engine;
    private readonly ILogger<PerTenantOutboundChannelQueue> _logger;
    private readonly ConcurrentDictionary<string, TenantChannelQueue> _queues = new(StringComparer.Ordinal);
    private readonly CancellationTokenSource _cts = new();

    public PerTenantOutboundChannelQueue(
        IWhatsAppEngine engine,
        ILogger<PerTenantOutboundChannelQueue> logger)
    {
        _engine = engine;
        _logger = logger;
    }

    public async Task<JsonElement> EnqueueAndAwaitAsync(
        string tenantId,
        string instanceId,
        string instanceName,
        string operation,
        JsonElement payload,
        CancellationToken ct)
    {
        var key = $"{tenantId}:{instanceId}";
        var queue = _queues.GetOrAdd(key, _ => new TenantChannelQueue(key, _engine, _logger, _cts.Token));

        var completion = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        var workItem = new OutboundWorkItem(
            tenantId,
            instanceId,
            instanceName,
            operation,
            payload,
            completion,
            ct);

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token);
        linkedCts.Token.Register(() => completion.TrySetCanceled(linkedCts.Token));

        await queue.Writer.WriteAsync(workItem, linkedCts.Token);

        return await completion.Task;
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        foreach (var queue in _queues.Values)
        {
            queue.Writer.TryComplete();
            await queue.ConsumerTask;
        }
        _queues.Clear();
        _cts.Dispose();
    }

    private sealed record OutboundWorkItem(
        string TenantId,
        string InstanceId,
        string InstanceName,
        string Operation,
        JsonElement Payload,
        TaskCompletionSource<JsonElement> Completion,
        CancellationToken CancellationToken);

    private sealed class TenantChannelQueue
    {
        private readonly Channel<OutboundWorkItem> _channel;
        public ChannelWriter<OutboundWorkItem> Writer => _channel.Writer;
        public Task ConsumerTask { get; }

        public TenantChannelQueue(
            string queueKey,
            IWhatsAppEngine engine,
            ILogger logger,
            CancellationToken globalCt)
        {
            _channel = Channel.CreateBounded<OutboundWorkItem>(new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            });

            ConsumerTask = Task.Run(() => ConsumeQueueAsync(queueKey, _channel.Reader, engine, logger, globalCt), globalCt);
        }

        private static async Task ConsumeQueueAsync(
            string queueKey,
            ChannelReader<OutboundWorkItem> reader,
            IWhatsAppEngine engine,
            ILogger logger,
            CancellationToken globalCt)
        {
            while (await reader.WaitToReadAsync(globalCt))
            {
                while (reader.TryRead(out var item))
                {
                    if (item.CancellationToken.IsCancellationRequested)
                    {
                        item.Completion.TrySetCanceled(item.CancellationToken);
                        continue;
                    }

                    try
                    {
                        logger.LogDebug("[{QueueKey}] Dispatching message operation '{Operation}' to instance '{Instance}'",
                            queueKey, item.Operation, item.InstanceName);

                        var result = await engine.SendAsync(item.InstanceId, item.Operation, item.Payload, item.CancellationToken);
                        item.Completion.TrySetResult(result);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "[{QueueKey}] Failed dispatching message operation '{Operation}'", queueKey, item.Operation);
                        item.Completion.TrySetException(ex);
                    }

                    // ── Safe Anti-Ban Jitter (3000ms - 6000ms) ───────────────────────────
                    // Spacing outbound operations safely to mimic human behavior and evade WhatsApp detection algorithms.
                    var jitterDelayMs = Random.Shared.Next(3000, 6001);
                    logger.LogDebug("[{QueueKey}] Anti-ban throttler sleeping for {Delay}ms before next message", queueKey, jitterDelayMs);

                    try
                    {
                        await Task.Delay(jitterDelayMs, globalCt);
                    }
                    catch (OperationCanceledException) when (globalCt.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }
        }
    }
}
