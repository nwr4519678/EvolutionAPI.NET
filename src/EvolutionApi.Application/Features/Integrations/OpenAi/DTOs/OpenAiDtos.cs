namespace EvolutionApi.Application.Features.Integrations.OpenAi.DTOs;

public sealed record SetOpenAiDto(
    bool Enabled = false,
    string? ApiKey = null,
    string? Model = null,
    string? SystemPrompt = null,
    int? MaxTokens = null,
    double? Temperature = null,
    bool? SpeechToText = null);

public sealed record StartOpenAiDto(
    string RemoteJid = "");
