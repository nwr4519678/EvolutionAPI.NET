using AutoMapper;
using EvolutionApi.Application.Features.Chat.Commands.ArchiveChat;
using EvolutionApi.Application.Features.Chat.Commands.DeleteMessage;
using EvolutionApi.Application.Features.Chat.Commands.MarkMessageRead;
using EvolutionApi.Application.Features.Chat.DTOs;
using EvolutionApi.Application.Features.Chat.Queries.CheckWhatsAppNumbers;
using EvolutionApi.Application.Features.Chat.Queries.FetchProfilePicture;

namespace EvolutionApi.Application.Features.Chat.Mappings;

/// <summary>
/// AutoMapper profile for the Chat feature.
/// All Commands and Queries that require InstanceName receive it via ctx.Items["instanceName"].
/// </summary>
public sealed class ChatMappingProfile : Profile
{
    public ChatMappingProfile()
    {
        // ── Queries ───────────────────────────────────────────────────────────────
        CreateMap<CheckWhatsAppNumbersDto, CheckWhatsAppNumbersQuery>()
            .ConstructUsing((dto, ctx) =>
                new CheckWhatsAppNumbersQuery(GetInstanceName(ctx), dto.Numbers));

        CreateMap<FetchProfilePictureDto, FetchProfilePictureQuery>()
            .ConstructUsing((dto, ctx) =>
                new FetchProfilePictureQuery(GetInstanceName(ctx), dto.Number, dto.Jid));

        // ── Commands ─────────────────────────────────────────────────────────────
        CreateMap<MarkMessageReadDto, MarkMessageReadCommand>()
            .ConstructUsing((dto, ctx) =>
            {
                var keys = dto.ReadMessages?
                    .Select(m => new ReadMessageKey(m.RemoteJid, m.Id, m.Participant))
                    .ToArray() ?? [];
                return new MarkMessageReadCommand(GetInstanceName(ctx), keys);
            });

        CreateMap<ArchiveChatDto, ArchiveChatCommand>()
            .ConstructUsing((dto, ctx) =>
                new ArchiveChatCommand(GetInstanceName(ctx), dto.Chat, dto.Archive));

        CreateMap<DeleteMessageDto, DeleteMessageCommand>()
            .ConstructUsing((dto, ctx) =>
                new DeleteMessageCommand(GetInstanceName(ctx), dto.RemoteJid, dto.Id, dto.FromMe, dto.Participant));
    }

    private static string GetInstanceName(ResolutionContext ctx) =>
        ctx.Items.TryGetValue("instanceName", out var v) ? v?.ToString() ?? string.Empty : string.Empty;
}
