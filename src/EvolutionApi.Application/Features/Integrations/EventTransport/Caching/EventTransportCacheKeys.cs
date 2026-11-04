namespace EvolutionApi.Application.Features.Integrations.EventTransport.Caching;

/// <summary>
/// Cache keys and invalidation tags for the EventTransport integration feature (RabbitMQ, SQS, NATS).
/// </summary>
public static class EventTransportCacheKeys
{
    public static string RabbitMqKey(string instanceName) => $"integrations:rabbitmq:{instanceName}";
    public static string SqsKey(string instanceName) => $"integrations:sqs:{instanceName}";
    public static string NatsKey(string instanceName) => $"integrations:nats:{instanceName}";

    public static string RabbitMqTag(string instanceName) => $"instance:{instanceName}:rabbitmq";
    public static string SqsTag(string instanceName) => $"instance:{instanceName}:sqs";
    public static string NatsTag(string instanceName) => $"instance:{instanceName}:nats";

    public static readonly TimeSpan Expiration = TimeSpan.FromMinutes(15);
}
