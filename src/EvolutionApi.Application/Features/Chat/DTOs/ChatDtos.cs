using EvolutionApi.Application.Features.Chat.Commands.MarkMessageRead;

namespace EvolutionApi.Application.Features.Chat.DTOs;

public sealed record CheckWhatsAppNumbersDto(
    string[]? Numbers = null);

public sealed record MarkMessageReadDto(
    ReadMessageKey[]? ReadMessages = null);

public sealed record ArchiveChatDto(
    string Chat = "",
    bool Archive = true);

public sealed record DeleteMessageDto(
    string RemoteJid = "",
    string Id = "",
    bool FromMe = true,
    string? Participant = null);

public sealed record FetchProfilePictureDto(
    string? Number = null,
    string? Jid = null);
