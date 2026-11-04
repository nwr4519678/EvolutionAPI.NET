using AutoMapper;
using EvolutionApi.Application.Features.Calls.Commands.OfferCall;
using EvolutionApi.Application.Features.Calls.DTOs;
using System.Text.Json;

namespace EvolutionApi.Application.Features.Calls.Mappings;

/// <summary>
/// AutoMapper profile for the Calls feature.
/// Converts JsonNode? (DTO) → JsonElement (command) and injects instanceName from ctx.Items.
/// </summary>
public sealed class CallMappingProfile : Profile
{
    public CallMappingProfile()
    {
        CreateMap<OfferCallDto, OfferCallCommand>()
            .ConstructUsing((dto, ctx) =>
            {
                var instanceName = ctx.Items.TryGetValue("instanceName", out var v)
                    ? v?.ToString() ?? string.Empty
                    : string.Empty;

                var payload = dto.Payload is not null
                    ? JsonSerializer.SerializeToElement(dto.Payload)
                    : default;

                return new OfferCallCommand(instanceName, payload);
            });
    }
}
