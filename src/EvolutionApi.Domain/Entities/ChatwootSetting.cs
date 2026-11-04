using EvolutionApi.Domain.Common;

namespace EvolutionApi.Domain.Entities;

/// <summary>
/// Chatwoot integration settings.
/// Pure POCO entity with zero business logic.
/// </summary>
public sealed class ChatwootSetting : BaseEntity
{
    public string InstanceId { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string AccountId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string NameInbox { get; set; } = string.Empty;
    public bool AutoCreate { get; set; } = true;
    public bool ImportContacts { get; set; }
    public bool ImportMessages { get; set; }
    public int DaysLimitImportMessages { get; set; } = 30;
    public bool SignMsg { get; set; }
    public bool ReOpenConversation { get; set; }
    public bool ConversationPending { get; set; }
    public bool MergeBrazilContacts { get; set; }

    // Navigation property
    public Instance? Instance { get; set; }
}
