namespace EvolutionApi.Application.Features.Integrations.EventTransport.DTOs;

public sealed record SetRabbitMqDto(
    bool Enabled = false,
    string? Uri = null,
    string? Exchange = null,
    bool? GlobalEvents = null,
    string[]? Events = null);

public sealed record SetSqsDto(
    bool Enabled = false,
    string? AccessKeyId = null,
    string? SecretAccessKey = null,
    string? AccountId = null,
    string? Region = null,
    string[]? Events = null);

public sealed record SetNatsDto(
    bool Enabled = false,
    string? Uri = null,
    string[]? Events = null);
