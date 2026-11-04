using EvolutionApi.Application.Common.Abstractions;
using EvolutionApi.Application.Common.Caching;
using EvolutionApi.Application.Features.Integrations.Chatwoot.Caching;
using EvolutionApi.Domain.Interfaces;
using MediatR;

namespace EvolutionApi.Application.Features.Integrations.Chatwoot.Queries.FindChatwoot;

public sealed record FindChatwootQuery(string InstanceName) : IQuery<FindChatwootResult>, ICacheableQuery
{
    public string CacheKey => ChatwootCacheKeys.Key(InstanceName);
    public TimeSpan? Expiration => ChatwootCacheKeys.Expiration;
    public string? CacheTag => ChatwootCacheKeys.Tag(InstanceName);
}

public sealed record FindChatwootResult(string InstanceName, ChatwootSettingDto Chatwoot);

public sealed record ChatwootSettingDto(
    bool Enabled = false,
    string? AccountId = null,
    string? Token = null,
    string? Url = null,
    string? NameInbox = null,
    bool? AutoCreate = null,
    bool? ImportContacts = null,
    bool? ImportMessages = null,
    int? DaysLimitImportMessages = null,
    bool? SignMsg = null,
    bool? ReOpenConversation = null,
    bool? ConversationPending = null,
    bool? MergeBrazilContacts = null);

public sealed class FindChatwootQueryHandler(
    IInstanceRepository instances,
    IIntegrationRepository integrations) : IRequestHandler<FindChatwootQuery, FindChatwootResult>
{
    public async Task<FindChatwootResult> Handle(FindChatwootQuery query, CancellationToken ct)
    {
        var instance = await instances.GetByNameAsync(query.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{query.InstanceName}' not found.");

        var setting = await integrations.GetChatwootAsync(instance.Id, ct);
        var dto = setting is not null
            ? new ChatwootSettingDto(
                setting.Enabled,
                setting.AccountId,
                setting.Token,
                setting.Url,
                setting.NameInbox,
                setting.AutoCreate,
                setting.ImportContacts,
                setting.ImportMessages,
                setting.DaysLimitImportMessages,
                setting.SignMsg,
                setting.ReOpenConversation,
                setting.ConversationPending,
                setting.MergeBrazilContacts)
            : new ChatwootSettingDto();

        return new FindChatwootResult(query.InstanceName, dto);
    }
}

