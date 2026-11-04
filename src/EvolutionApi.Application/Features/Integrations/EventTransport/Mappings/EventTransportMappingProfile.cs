using AutoMapper;
using EvolutionApi.Application.Features.Integrations.EventTransport.Commands.SetNats;
using EvolutionApi.Application.Features.Integrations.EventTransport.Commands.SetRabbitMq;
using EvolutionApi.Application.Features.Integrations.EventTransport.Commands.SetSqs;
using EvolutionApi.Application.Features.Integrations.EventTransport.DTOs;

namespace EvolutionApi.Application.Features.Integrations.EventTransport.Mappings;

/// <summary>
/// AutoMapper profile for the EventTransport integration feature (RabbitMQ, SQS, NATS).
/// All commands receive InstanceName via ctx.Items["instanceName"].
/// </summary>
public sealed class EventTransportMappingProfile : Profile
{
    public EventTransportMappingProfile()
    {
        CreateMap<SetRabbitMqDto, SetRabbitMqCommand>()
            .ConstructUsing((dto, ctx) =>
                new SetRabbitMqCommand(GetInstanceName(ctx), dto.Enabled,
                    dto.Uri, dto.Exchange, dto.GlobalEvents, dto.Events));

        CreateMap<SetSqsDto, SetSqsCommand>()
            .ConstructUsing((dto, ctx) =>
                new SetSqsCommand(GetInstanceName(ctx), dto.Enabled,
                    dto.AccessKeyId, dto.SecretAccessKey, dto.AccountId, dto.Region, dto.Events));

        CreateMap<SetNatsDto, SetNatsCommand>()
            .ConstructUsing((dto, ctx) =>
                new SetNatsCommand(GetInstanceName(ctx), dto.Enabled, dto.Uri, dto.Events));
    }

    private static string GetInstanceName(ResolutionContext ctx) =>
        ctx.Items.TryGetValue("instanceName", out var v) ? v?.ToString() ?? string.Empty : string.Empty;
}
