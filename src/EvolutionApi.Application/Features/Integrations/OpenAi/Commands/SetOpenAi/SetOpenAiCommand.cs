using EvolutionApi.Application.Common.Abstractions;
using EvolutionApi.Application.Features.Integrations.OpenAi.Caching;
using EvolutionApi.Domain.Entities;
using EvolutionApi.Domain.Interfaces;

namespace EvolutionApi.Application.Features.Integrations.OpenAi.Commands.SetOpenAi;

// ── Command ───────────────────────────────────────────────────────────────────

public sealed record SetOpenAiCommand(
    string  InstanceName,
    bool    Enabled      = false,
    string? ApiKey       = null,
    string? Model        = null,
    string? SystemPrompt = null,
    int?    MaxTokens    = null,
    double? Temperature  = null,
    bool?   SpeechToText = null) : ICommand<object>;

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class SetOpenAiCommandHandler(
    IInstanceRepository instances,
    IIntegrationRepository integrations,
    ICacheService cache) : MediatR.IRequestHandler<SetOpenAiCommand, object>
{
    public async Task<object> Handle(SetOpenAiCommand cmd, CancellationToken ct)
    {
        var instance = await instances.GetByNameAsync(cmd.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{cmd.InstanceName}' not found.");

        var existing = await integrations.GetOpenAiAsync(instance.Id, ct)
            ?? new OpenAiSetting { InstanceId = instance.Id };

        existing.Enabled      = cmd.Enabled;
        existing.ApiKey       = cmd.ApiKey       ?? existing.ApiKey;
        existing.Model        = cmd.Model        ?? existing.Model;
        existing.SystemPrompt = cmd.SystemPrompt ?? existing.SystemPrompt;
        existing.MaxTokens    = cmd.MaxTokens    ?? existing.MaxTokens;
        existing.Temperature  = cmd.Temperature  ?? existing.Temperature;
        existing.SpeechToText = cmd.SpeechToText ?? existing.SpeechToText;
        existing.UpdatedAt    = DateTime.UtcNow;

        await integrations.AddOrUpdateOpenAiAsync(existing, ct);
        await cache.RemoveByTagAsync(OpenAiCacheKeys.Tag(cmd.InstanceName), ct);

        // Mask API key in response
        return new { instanceName = cmd.InstanceName, openai = cmd with { ApiKey = "****" } };
    }
}
