namespace EvolutionApi.Application.Features.Settings.DTOs;

public sealed record SetSettingsDto(
    bool? RejectCall = null,
    string? MsgCall = null,
    bool? GroupsIgnore = null,
    bool? AlwaysOnline = null,
    bool? ReadMessages = null,
    bool? ReadStatus = null,
    bool? SyncFullHistory = null,
    string? WavoipToken = null);
