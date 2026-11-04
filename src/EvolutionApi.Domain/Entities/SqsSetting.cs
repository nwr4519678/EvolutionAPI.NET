using EvolutionApi.Domain.Common;

namespace EvolutionApi.Domain.Entities;

/// <summary>
/// AWS SQS event publishing configuration.
/// Pure POCO entity with zero business logic.
/// </summary>
public sealed class SqsSetting : BaseEntity
{
    public string InstanceId { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string Region { get; set; } = "us-east-1";
    public string EventsJson { get; set; } = "[]";

    // Navigation property
    public Instance? Instance { get; set; }
}
