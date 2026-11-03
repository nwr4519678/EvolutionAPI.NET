namespace EvolutionApi.Application.Features.Webhooks.DTOs;

public sealed record SetWebhookDto(
    bool Enabled = false,
    string Url = "",
    string[]? Events = null,
    Dictionary<string, string>? Headers = null,
    bool ByEvents = false,
    bool Base64 = false);
