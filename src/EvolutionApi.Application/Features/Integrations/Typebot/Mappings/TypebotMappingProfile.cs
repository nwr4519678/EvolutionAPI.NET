using AutoMapper;
using EvolutionApi.Application.Features.Integrations.Typebot.Commands.SetTypebot;
using EvolutionApi.Application.Features.Integrations.Typebot.Commands.TypebotChangeStatus;
using EvolutionApi.Application.Features.Integrations.Typebot.Commands.TypebotStart;
using EvolutionApi.Application.Features.Integrations.Typebot.DTOs;

namespace EvolutionApi.Application.Features.Integrations.Typebot.Mappings;

/// <summary>
/// AutoMapper profile for the Typebot integration feature.
/// All commands receive InstanceName via ctx.Items["instanceName"].
/// </summary>
public sealed class TypebotMappingProfile : Profile
{
    public TypebotMappingProfile()
    {
        CreateMap<SetTypebotDto, SetTypebotCommand>()
            .ConstructUsing((dto, ctx) =>
            {
                var instance = GetInstanceName(ctx);
                return new SetTypebotCommand(instance, dto.Enabled, dto.Url, dto.TypebotId,
                    dto.Expire, dto.KeywordFinish, dto.DelayMessage, dto.UnknownMessage, dto.ListeningFromMe);
            });

        CreateMap<TypebotStartDto, TypebotStartCommand>()
            .ConstructUsing((dto, ctx) =>
                new TypebotStartCommand(GetInstanceName(ctx), dto.RemoteJid, dto.Url));

        CreateMap<TypebotStatusDto, TypebotChangeStatusCommand>()
            .ConstructUsing((dto, ctx) =>
                new TypebotChangeStatusCommand(GetInstanceName(ctx), dto.RemoteJid, dto.Status));
    }

    private static string GetInstanceName(ResolutionContext ctx) =>
        ctx.Items.TryGetValue("instanceName", out var v) ? v?.ToString() ?? string.Empty : string.Empty;
}
