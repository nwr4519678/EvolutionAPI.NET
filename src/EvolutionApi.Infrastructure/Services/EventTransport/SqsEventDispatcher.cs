using Amazon.SQS;
using Amazon.SQS.Model;
using EvolutionApi.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EvolutionApi.Infrastructure.Services.EventTransport;

/// <summary>
/// Publishes WhatsApp events to AWS SQS using the official AWSSDK.SQS package (via MassTransit.AmazonSQS).
/// Per-instance credentials are loaded from the database at runtime.
/// </summary>
public sealed class SqsEventDispatcher(
    IServiceScopeFactory scopeFactory,
    ILogger<SqsEventDispatcher> logger)
{
    private static readonly JsonSerializerOptions Jo = new(JsonSerializerDefaults.Web);

    public async Task PublishAsync(string instanceId, string eventName, object payload, CancellationToken ct = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var integrations = scope.ServiceProvider.GetRequiredService<IIntegrationRepository>();

        var cfg = await integrations.GetSqsAsync(instanceId, ct);
        if (cfg is null || !cfg.Enabled)
        {
            logger.LogDebug("SQS not configured or disabled for instance {InstanceId}", instanceId);
            return;
        }

        try
        {
            var sqsClient = new AmazonSQSClient(
                cfg.AccessKeyId,
                cfg.SecretAccessKey,
                Amazon.RegionEndpoint.GetBySystemName(cfg.Region ?? "us-east-1"));

            // Get or create the queue for this instance
            var queueName = $"evolution-{instanceId[..Math.Min(instanceId.Length, 20)]}";
            var queueUrl = await EnsureQueueAsync(sqsClient, queueName, ct);

            var message = JsonSerializer.Serialize(new { instanceId, eventName, payload }, Jo);
            await sqsClient.SendMessageAsync(new SendMessageRequest(queueUrl, message), ct);

            logger.LogInformation("SQS event published: {Event} for instance {InstanceId}", eventName, instanceId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish SQS event {Event} for instance {InstanceId}", eventName, instanceId);
        }
    }

    private static async Task<string> EnsureQueueAsync(AmazonSQSClient client, string queueName, CancellationToken ct)
    {
        try
        {
            var response = await client.GetQueueUrlAsync(queueName, ct);
            return response.QueueUrl;
        }
        catch (QueueDoesNotExistException)
        {
            var created = await client.CreateQueueAsync(queueName, ct);
            return created.QueueUrl;
        }
    }
}
