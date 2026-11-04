using EvolutionApi.Application.Common.Abstractions;
using EvolutionApi.Application.Common.Caching;
using EvolutionApi.Application.Features.Integrations.OpenAi.Caching;
using EvolutionApi.Domain.Interfaces;
using MediatR;

namespace EvolutionApi.Application.Features.Integrations.OpenAi.Queries.FindOpenAi;

public sealed record FindOpenAiQuery(string InstanceName) : IQuery<FindOpenAiResult>, ICacheableQuery
{
    public string CacheKey => OpenAiCacheKeys.Key(InstanceName);
    public TimeSpan? Expiration => OpenAiCacheKeys.Expiration;
    public string? CacheTag => OpenAiCacheKeys.Tag(InstanceName);
}

public sealed record FindOpenAiResult(string InstanceName, OpenAiSettingDto OpenAi);

public sealed record OpenAiSettingDto(
    bool Enabled = false,
    string? ApiKey = null,
    string? Model = null,
    string? SystemPrompt = null,
    int? MaxTokens = null,
    double? Temperature = null,
    bool? SpeechToText = null);

public sealed class FindOpenAiQueryHandler(
    IInstanceRepository instances,
    IIntegrationRepository integrations) : IRequestHandler<FindOpenAiQuery, FindOpenAiResult>
{
    public async Task<FindOpenAiResult> Handle(FindOpenAiQuery query, CancellationToken ct)
    {
        var instance = await instances.GetByNameAsync(query.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{query.InstanceName}' not found.");

        var s = await integrations.GetOpenAiAsync(instance.Id, ct);
        var dto = s is not null
            ? new OpenAiSettingDto(
                s.Enabled,
                "****",
                s.Model,
                s.SystemPrompt,
                s.MaxTokens,
                s.Temperature,
                s.SpeechToText)
            : new OpenAiSettingDto();

        return new FindOpenAiResult(query.InstanceName, dto);
    }
}
