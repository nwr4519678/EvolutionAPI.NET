using EvolutionApi.Domain.Common;

namespace EvolutionApi.Domain.Entities;

public sealed class PlatformWebhookDelivery : BaseEntity
{
    public required string EndpointId { get; set; }
    public required string TenantId { get; set; }
    public required string EventType { get; set; }
    public required string Payload { get; set; }
    public int? HttpStatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public int AttemptCount { get; set; } = 1;
    public DateTimeOffset? NextRetryAt { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }

    // Navigation
    public WebhookEndpoint? Endpoint { get; set; }
}
