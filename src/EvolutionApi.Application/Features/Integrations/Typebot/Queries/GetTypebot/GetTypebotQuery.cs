using EvolutionApi.Application.Common.Abstractions;
using EvolutionApi.Application.Common.Caching;
using EvolutionApi.Application.Features.Integrations.Typebot.Caching;
using EvolutionApi.Domain.Interfaces;
using MediatR;

namespace EvolutionApi.Application.Features.Integrations.Typebot.Queries.GetTypebot;

public sealed record GetTypebotQuery(string InstanceName) : IQuery<GetTypebotResult>, ICacheableQuery
{
    public string CacheKey => TypebotCacheKeys.Key(InstanceName);
    public TimeSpan? Expiration => TypebotCacheKeys.Expiration;
    public string? CacheTag => TypebotCacheKeys.Tag(InstanceName);
}

public sealed record GetTypebotResult(string InstanceName, TypebotSettingDto Typebot);

public sealed record TypebotSettingDto(
    bool Enabled = false,
    string? Url = null,
    string? TypebotId = null,
    int? Expire = null,
    string? KeywordFinish = null,
    int? DelayMessage = null,
    string? UnknownMessage = null,
    bool? ListeningFromMe = null);

public sealed class GetTypebotQueryHandler(
    IInstanceRepository instances,
    IIntegrationRepository integrations) : IRequestHandler<GetTypebotQuery, GetTypebotResult>
{
    public async Task<GetTypebotResult> Handle(GetTypebotQuery query, CancellationToken ct)
    {
        var instance = await instances.GetByNameAsync(query.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{query.InstanceName}' not found.");

        var setting = await integrations.GetTypebotAsync(instance.Id, ct);
        var dto = setting is not null
            ? new TypebotSettingDto(
                setting.Enabled,
                setting.Url,
                setting.TypebotId,
                setting.Expire,
                setting.KeywordFinish,
                setting.DelayMessage,
                setting.UnknownMessage,
                setting.ListeningFromMe)
            : new TypebotSettingDto();

        return new GetTypebotResult(query.InstanceName, dto);
    }
}

