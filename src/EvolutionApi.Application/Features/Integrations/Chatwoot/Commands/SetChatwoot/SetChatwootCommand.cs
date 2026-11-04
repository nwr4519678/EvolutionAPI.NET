using EvolutionApi.Application.Common.Abstractions;
using EvolutionApi.Application.Features.Integrations.Chatwoot.Caching;
using EvolutionApi.Domain.Entities;
using EvolutionApi.Domain.Interfaces;

namespace EvolutionApi.Application.Features.Integrations.Chatwoot.Commands.SetChatwoot;

// ── Command ───────────────────────────────────────────────────────────────────

public sealed record SetChatwootCommand(
    string  InstanceName,
    bool    Enabled                  = false,
    string? AccountId                = null,
    string? Token                    = null,
    string? Url                      = null,
    string? NameInbox                = null,
    bool?   AutoCreate               = null,
    bool?   ImportContacts           = null,
    bool?   ImportMessages           = null,
    int?    DaysLimitImportMessages  = null,
    bool?   SignMsg                  = null,
    bool?   ReOpenConversation       = null,
    bool?   ConversationPending      = null,
    bool?   MergeBrazilContacts      = null) : ICommand<object>;

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class SetChatwootCommandHandler(
    IInstanceRepository instances,
    IIntegrationRepository integrations,
    ICacheService cache) : MediatR.IRequestHandler<SetChatwootCommand, object>
{
    public async Task<object> Handle(SetChatwootCommand cmd, CancellationToken ct)
    {
        var instance = await instances.GetByNameAsync(cmd.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{cmd.InstanceName}' not found.");

        var existing = await integrations.GetChatwootAsync(instance.Id, ct)
            ?? new ChatwootSetting { InstanceId = instance.Id };

        existing.Enabled                 = cmd.Enabled;
        existing.AccountId               = cmd.AccountId               ?? existing.AccountId;
        existing.Token                   = cmd.Token                   ?? existing.Token;
        existing.Url                     = cmd.Url                     ?? existing.Url;
        existing.NameInbox               = cmd.NameInbox               ?? existing.NameInbox;
        existing.AutoCreate              = cmd.AutoCreate              ?? existing.AutoCreate;
        existing.ImportContacts          = cmd.ImportContacts          ?? existing.ImportContacts;
        existing.ImportMessages          = cmd.ImportMessages          ?? existing.ImportMessages;
        existing.DaysLimitImportMessages = cmd.DaysLimitImportMessages ?? existing.DaysLimitImportMessages;
        existing.SignMsg                 = cmd.SignMsg                 ?? existing.SignMsg;
        existing.ReOpenConversation      = cmd.ReOpenConversation      ?? existing.ReOpenConversation;
        existing.ConversationPending     = cmd.ConversationPending     ?? existing.ConversationPending;
        existing.MergeBrazilContacts     = cmd.MergeBrazilContacts     ?? existing.MergeBrazilContacts;
        existing.UpdatedAt               = DateTime.UtcNow;

        await integrations.AddOrUpdateChatwootAsync(existing, ct);
        await cache.RemoveByTagAsync(ChatwootCacheKeys.Tag(cmd.InstanceName), ct);

        return new { instanceName = cmd.InstanceName, chatwoot = cmd };
    }
}
