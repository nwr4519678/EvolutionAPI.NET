using AutoMapper;
using EvolutionApi.Application.Features.Groups.Commands.CreateGroup;
using EvolutionApi.Application.Features.Groups.Commands.UpdateGroupParticipants;
using EvolutionApi.Application.Features.Groups.Commands.UpdateGroupSubject;
using EvolutionApi.Application.Features.Groups.DTOs;

namespace EvolutionApi.Application.Features.Groups.Mappings;

/// <summary>
/// AutoMapper profile for the Groups feature.
/// All commands receive InstanceName via ctx.Items["instanceName"].
/// </summary>
public sealed class GroupMappingProfile : Profile
{
    public GroupMappingProfile()
    {
        CreateMap<CreateGroupDto, CreateGroupCommand>()
            .ConstructUsing((dto, ctx) =>
                new CreateGroupCommand(GetInstanceName(ctx), dto.Subject, dto.Participants));

        CreateMap<UpdateGroupParticipantsDto, UpdateGroupParticipantsCommand>()
            .ConstructUsing((dto, ctx) =>
                new UpdateGroupParticipantsCommand(GetInstanceName(ctx), dto.GroupJid, dto.Participants, dto.Action));

        CreateMap<UpdateGroupSubjectDto, UpdateGroupSubjectCommand>()
            .ConstructUsing((dto, ctx) =>
                new UpdateGroupSubjectCommand(GetInstanceName(ctx), dto.GroupJid, dto.Subject));
    }

    private static string GetInstanceName(ResolutionContext ctx) =>
        ctx.Items.TryGetValue("instanceName", out var v) ? v?.ToString() ?? string.Empty : string.Empty;
}
