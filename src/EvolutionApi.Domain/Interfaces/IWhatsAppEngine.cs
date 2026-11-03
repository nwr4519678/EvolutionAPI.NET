using System.Text.Json;

namespace EvolutionApi.Domain.Interfaces;

/// <summary>
/// WhatsApp engine contract defining all WhatsApp session and messaging operations.
/// </summary>
public interface IWhatsAppEngine
{
    // ── Lifecycle ──────────────────────────────────────────────────────────────
    Task StartAsync(string instanceId, string instanceName, string? number, CancellationToken ct);
    Task StopAsync(string instanceId, CancellationToken ct);
    Task<JsonElement> ConnectAsync(string instanceId, string? number, CancellationToken ct);
    Task<string> GetStateAsync(string instanceId, CancellationToken ct);

    // ── Presence ──────────────────────────────────────────────────────────────
    Task<JsonElement> SetPresenceAsync(string instanceId, string presenceType, CancellationToken ct);
    Task<JsonElement> SendChatStateAsync(string instanceId, string jid, string state, CancellationToken ct);

    // ── Messages ───────────────────────────────────────────────────────────────
    Task<JsonElement> SendAsync(string instanceId, string operation, JsonElement payload, CancellationToken ct);

    // ── Chat & Profile operations ──────────────────────────────────────────────
    Task<JsonElement> CheckWhatsAppNumbersAsync(string instanceId, string[] numbers, CancellationToken ct);
    Task<JsonElement> MarkMessagesAsReadAsync(string instanceId, string jid, string[] ids, string? participant, CancellationToken ct);
    Task DeleteMessageForEveryoneAsync(string instanceId, string jid, string msgId, bool fromMe, string? participant, CancellationToken ct);
    Task<JsonElement> FetchProfilePictureUrlAsync(string instanceId, string jid, CancellationToken ct);
    Task<JsonElement> FetchPrivacySettingsAsync(string instanceId, CancellationToken ct);
    Task UpdatePrivacySettingsAsync(string instanceId, Dictionary<string, string> categories, CancellationToken ct);
    Task UpdateProfileNameAsync(string instanceId, string name, CancellationToken ct);
    Task UpdateProfileStatusAsync(string instanceId, string status, CancellationToken ct);
    Task UpdateBlockStatusAsync(string instanceId, string jid, string action, CancellationToken ct);
    Task<JsonElement> FetchProfileAsync(string instanceId, string jid, CancellationToken ct);
    Task<JsonElement> FetchBusinessProfileAsync(string instanceId, string jid, CancellationToken ct);
    Task<JsonElement> UpdateProfilePictureAsync(string instanceId, string jid, byte[] jpegBytes, CancellationToken ct);
    Task<JsonElement> RemoveProfilePictureAsync(string instanceId, string jid, CancellationToken ct);

    // ── Group operations ───────────────────────────────────────────────────────
    Task<JsonElement> CreateGroupAsync(string instanceId, string subject, string[] participants, CancellationToken ct);
    Task<JsonElement> FetchGroupMetadataAsync(string instanceId, string groupJid, CancellationToken ct);
    Task<JsonElement> FetchAllGroupsAsync(string instanceId, CancellationToken ct);
    Task<JsonElement> UpdateGroupParticipantsAsync(string instanceId, string groupJid, string[] participants, string action, CancellationToken ct);
    Task UpdateGroupSubjectAsync(string instanceId, string groupJid, string subject, CancellationToken ct);
    Task UpdateGroupDescriptionAsync(string instanceId, string groupJid, string description, CancellationToken ct);
    Task<JsonElement> FetchGroupInviteCodeAsync(string instanceId, string groupJid, CancellationToken ct);
    Task<JsonElement> RevokeGroupInviteCodeAsync(string instanceId, string groupJid, CancellationToken ct);
    Task<JsonElement> FetchGroupInviteInfoAsync(string instanceId, string code, CancellationToken ct);
    Task<JsonElement> AcceptGroupInviteAsync(string instanceId, string code, CancellationToken ct);
    Task UpdateGroupSettingAsync(string instanceId, string groupJid, string setting, bool enable, CancellationToken ct);
    Task ToggleEphemeralAsync(string instanceId, string groupJid, int durationSeconds, CancellationToken ct);
    Task LeaveGroupAsync(string instanceId, string groupJid, CancellationToken ct);
    Task<JsonElement> UpdateGroupPictureAsync(string instanceId, string groupJid, byte[] jpegBytes, CancellationToken ct);
    Task<JsonElement> SendGroupInviteAsync(string instanceId, string groupJid, string[] numbers, CancellationToken ct);

    // ── Calls ──────────────────────────────────────────────────────────────────
    Task<JsonElement> OfferCallAsync(string instanceId, JsonElement payload, CancellationToken ct);

    // ── Business / Labels ──────────────────────────────────────────────────────
    Task<JsonElement> FetchLabelsAsync(string instanceId, CancellationToken ct);
    Task<JsonElement> HandleLabelAsync(string instanceId, string label, string chatId, string action, CancellationToken ct);
    Task<JsonElement> CreateLabelAsync(string instanceId, string name, string color, CancellationToken ct);
    Task<JsonElement> UpdateLabelAsync(string instanceId, string labelId, string? name, string? color, CancellationToken ct);
    Task<JsonElement> DeleteLabelAsync(string instanceId, string labelId, CancellationToken ct);
}
