using AutoMapper;
using EvolutionApi.Application.Features.Proxy.Commands.SetProxy;
using EvolutionApi.Application.Features.Proxy.DTOs;

namespace EvolutionApi.Application.Features.Proxy.Mappings;

/// <summary>
/// AutoMapper profile for the Proxy feature.
/// InstanceName is injected via ctx.Items["instanceName"].
/// </summary>
public sealed class ProxyMappingProfile : Profile
{
    public ProxyMappingProfile()
    {
        CreateMap<SetProxyDto, SetProxyCommand>()
            .ConstructUsing((dto, ctx) =>
            {
                var instanceName = ctx.Items.TryGetValue("instanceName", out var v)
                    ? v?.ToString() ?? string.Empty : string.Empty;

                return new SetProxyCommand(
                    instanceName,
                    dto.Enabled,
                    dto.Host,
                    dto.Port,
                    dto.Protocol,
                    dto.Username,
                    dto.Password);
            });
    }
}
