using AutoMapper;
using EvolutionApi.Application.Features.Labels.Commands.CreateLabel;
using EvolutionApi.Application.Features.Labels.Commands.DeleteLabel;
using EvolutionApi.Application.Features.Labels.Commands.HandleLabel;
using EvolutionApi.Application.Features.Labels.Commands.UpdateLabel;
using EvolutionApi.Application.Features.Labels.DTOs;

namespace EvolutionApi.Application.Features.Labels.Mappings;

/// <summary>
/// AutoMapper profile for Label feature.
/// All command constructors now require InstanceName as first parameter,
/// so mappings use ctx.Items["instanceName"] injected by the controller.
/// </summary>
public sealed class LabelMappingProfile : Profile
{
    public LabelMappingProfile()
    {
        CreateMap<CreateLabelDto, CreateLabelCommand>()
            .ConstructUsing((dto, ctx) =>
            {
                var instance = GetInstanceName(ctx);
                return new CreateLabelCommand(instance, dto.Name, dto.Color);
            });

        CreateMap<UpdateLabelDto, UpdateLabelCommand>()
            .ConstructUsing((dto, ctx) =>
            {
                var instance = GetInstanceName(ctx);
                return new UpdateLabelCommand(instance, dto.LabelId, dto.Name, dto.Color);
            });

        CreateMap<DeleteLabelDto, DeleteLabelCommand>()
            .ConstructUsing((dto, ctx) =>
            {
                var instance = GetInstanceName(ctx);
                return new DeleteLabelCommand(instance, dto.LabelId);
            });

        CreateMap<HandleLabelDto, HandleLabelCommand>()
            .ConstructUsing((dto, ctx) =>
            {
                var instance = GetInstanceName(ctx);
                return new HandleLabelCommand(instance, dto.Label, dto.ChatId, dto.Action);
            });
    }

    private static string GetInstanceName(ResolutionContext ctx) =>
        ctx.Items.TryGetValue("instanceName", out var v)
            ? v?.ToString() ?? string.Empty
            : string.Empty;
}
