namespace EvolutionApi.Application.Features.Integrations.Typebot.DTOs;

public sealed record SetTypebotDto(
    bool Enabled = false,
    string? Url = null,
    string? TypebotId = null,
    int? Expire = null,
    string? KeywordFinish = null,
    int? DelayMessage = null,
    string? UnknownMessage = null,
    bool? ListeningFromMe = null);

public sealed record TypebotStartDto(
    string RemoteJid = "",
    string? Url = null);

public sealed record TypebotStatusDto(
    string RemoteJid = "",
    string? Status = null);
