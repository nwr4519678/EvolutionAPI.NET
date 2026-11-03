namespace EvolutionApi.Application.Features.Integrations.Chatwoot.DTOs;

public sealed record SetChatwootDto(
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
