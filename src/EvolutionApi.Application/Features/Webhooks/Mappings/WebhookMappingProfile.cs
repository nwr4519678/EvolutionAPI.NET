using AutoMapper;
using EvolutionApi.Application.Features.Webhooks.Commands.SetWebhook;
using EvolutionApi.Application.Features.Webhooks.DTOs;

namespace EvolutionApi.Application.Features.Webhooks.Mappings;

public sealed class WebhookMappingProfile : Profile
{
    public WebhookMappingProfile()
    {
        CreateMap<SetWebhookDto, SetWebhookCommand>()
            .ConstructUsing((dto, ctx) =>
            {
                var instanceName = ctx.Items.TryGetValue("instanceName", out var v)
                    ? v?.ToString() ?? string.Empty
                    : string.Empty;

                return new SetWebhookCommand(
                    instanceName,
                    dto.Enabled,
                    dto.Url,
                    dto.Events,
                    dto.Headers,
                    dto.ByEvents,
                    dto.Base64);
            });
    }
}
