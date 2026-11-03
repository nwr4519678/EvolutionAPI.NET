using AutoMapper;
using EvolutionApi.Application.Features.Instances.Commands.ConnectInstance;
using EvolutionApi.Application.Features.Instances.Commands.CreateInstance;
using EvolutionApi.Application.Features.Instances.DTOs;
using EvolutionApi.Domain.Entities;

namespace EvolutionApi.Application.Features.Instances.Mappings;

public sealed class InstanceMappingProfile : Profile
{
    public InstanceMappingProfile()
    {
        CreateMap<CreateInstanceDto, CreateInstanceCommand>()
            .ConstructUsing(src => new CreateInstanceCommand(
                src.InstanceName,
                src.Token,
                src.Number,
                src.WebhookUrl,
                src.WebhookEvents,
                src.RejectCall,
                src.GroupsIgnore,
                src.AlwaysOnline,
                src.ReadMessages));

        CreateMap<Instance, InstanceResponseDto>()
            .ForMember(d => d.InstanceId, opt => opt.MapFrom(s => s.Id))
            .ForMember(d => d.InstanceName, opt => opt.MapFrom(s => s.Name))
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.ConnectionStatus))
            .ForMember(d => d.CreatedAtUtc, opt => opt.MapFrom(s => s.CreatedAt));

        CreateMap<CreateInstanceResult, InstanceResponseDto>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status));
    }
}
