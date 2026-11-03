namespace EvolutionApi.Application.Features.Instances.DTOs;

public sealed record CreateInstanceDto(
    string InstanceName,
    string Token,
    string? Number = null,
    string? WebhookUrl = null,
    string[]? WebhookEvents = null,
    bool RejectCall = false,
    bool GroupsIgnore = false,
    bool AlwaysOnline = false,
    bool ReadMessages = false);

public sealed record InstanceResponseDto(
    string InstanceId,
    string InstanceName,
    string Status,
    string? Number = null,
    string? ProfileName = null,
    string? ProfilePicUrl = null,
    DateTime CreatedAtUtc = default);

public sealed record ConnectInstanceDto(
    string? Number = null);
