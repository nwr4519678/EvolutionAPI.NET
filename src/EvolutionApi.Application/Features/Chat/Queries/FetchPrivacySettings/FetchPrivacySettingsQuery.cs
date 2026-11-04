using EvolutionApi.Application.Common.Abstractions;
using EvolutionApi.Application.Common.Caching;
using EvolutionApi.Application.Features.Chat.Caching;
using EvolutionApi.Domain.Interfaces;
using MediatR;
using System.Text.Json;

namespace EvolutionApi.Application.Features.Chat.Queries.FetchPrivacySettings;

public sealed record FetchPrivacySettingsQuery(string InstanceName) : IQuery<JsonElement>, ICacheableQuery
{
    public string CacheKey => ChatCacheKeys.PrivacySettings(InstanceName);
    public TimeSpan? Expiration => ChatCacheKeys.Expiration;
    public string? CacheTag => ChatCacheKeys.Tag(InstanceName);
}

public sealed class FetchPrivacySettingsQueryHandler(
    IInstanceRepository instances,
    IWhatsAppEngine engine) : IRequestHandler<FetchPrivacySettingsQuery, JsonElement>
{
    public async Task<JsonElement> Handle(FetchPrivacySettingsQuery query, CancellationToken ct)
    {
        var instance = await instances.GetByNameAsync(query.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{query.InstanceName}' not found.");
        return await engine.FetchPrivacySettingsAsync(instance.Id, ct);
    }
}
