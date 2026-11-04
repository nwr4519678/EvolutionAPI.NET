using System.Text.Json;
using AutoMapper;
using EvolutionApi.Application.Features.Messages.Commands.SendAgnosticMessage;
using EvolutionApi.Application.Features.Messages.Commands.SendMessage;
using EvolutionApi.Application.Features.Messages.DTOs;

namespace EvolutionApi.Application.Features.Messages.Mappings;

public sealed class MessageMappingProfile : Profile
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public MessageMappingProfile()
    {
        CreateMap<SendAgnosticMessageDto, SendAgnosticMessageCommand>();

        CreateMap<SendTextMessageDto, SendMessageCommand>()
            .ConstructUsing((dto, ctx) =>
            {
                var instanceName = ctx.Items.TryGetValue("instanceName", out var name) ? name?.ToString() ?? string.Empty : string.Empty;
                var payload = JsonSerializer.SerializeToElement(new
                {
                    number = dto.Number,
                    text = new { message = dto.Text },
                    delay = dto.Delay,
                    quoted = dto.QuotedMessageId != null ? new { key = dto.QuotedMessageId } : null,
                    mentioned = dto.Mentions
                }, JsonOpts);

                return new SendMessageCommand(instanceName, "sendText", payload);
            });

        CreateMap<SendMediaMessageDto, SendMessageCommand>()
            .ConstructUsing((dto, ctx) =>
            {
                var instanceName = ctx.Items.TryGetValue("instanceName", out var name) ? name?.ToString() ?? string.Empty : string.Empty;
                var operation = dto.MediaType?.ToLowerInvariant() switch
                {
                    "audio" => "sendWhatsAppAudio",
                    "sticker" => "sendSticker",
                    _ => "sendMedia"
                };

                var payload = JsonSerializer.SerializeToElement(new
                {
                    number = dto.Number,
                    mediatype = dto.MediaType ?? "image",
                    media = dto.Media,
                    caption = dto.Caption,
                    fileName = dto.FileName,
                    mimeType = dto.MimeType,
                    delay = dto.Delay
                }, JsonOpts);

                return new SendMessageCommand(instanceName, operation, payload);
            });

        CreateMap<SendLocationMessageDto, SendMessageCommand>()
            .ConstructUsing((dto, ctx) =>
            {
                var instanceName = ctx.Items.TryGetValue("instanceName", out var name) ? name?.ToString() ?? string.Empty : string.Empty;
                var payload = JsonSerializer.SerializeToElement(new
                {
                    number = dto.Number,
                    latitude = dto.Latitude,
                    longitude = dto.Longitude,
                    name = dto.Name,
                    address = dto.Address
                }, JsonOpts);

                return new SendMessageCommand(instanceName, "sendLocation", payload);
            });

        CreateMap<SendContactMessageDto, SendMessageCommand>()
            .ConstructUsing((dto, ctx) =>
            {
                var instanceName = ctx.Items.TryGetValue("instanceName", out var name) ? name?.ToString() ?? string.Empty : string.Empty;
                var payload = JsonSerializer.SerializeToElement(new
                {
                    number = dto.Number,
                    contact = new[]
                    {
                        new { fullName = dto.FullName, waNumber = dto.WaNumber }
                    }
                }, JsonOpts);

                return new SendMessageCommand(instanceName, "sendContact", payload);
            });

        CreateMap<SendWhatsAppAudioDto, SendMessageCommand>()
            .ConstructUsing((dto, ctx) =>
            {
                var instanceName = ctx.Items.TryGetValue("instanceName", out var name) ? name?.ToString() ?? string.Empty : string.Empty;
                var payload = JsonSerializer.SerializeToElement(new
                {
                    number = dto.Number,
                    audio = dto.Audio,
                    delay = dto.Delay,
                    mimetype = dto.MimeType ?? "audio/ogg; codecs=opus"
                }, JsonOpts);

                return new SendMessageCommand(instanceName, "sendWhatsAppAudio", payload);
            });

        CreateMap<SendRawMessageDto, SendMessageCommand>()
            .ConstructUsing((dto, ctx) =>
            {
                var instanceName = ctx.Items.TryGetValue("instanceName", out var name) ? name?.ToString() ?? string.Empty : string.Empty;
                var payload = dto.Payload is not null
                    ? System.Text.Json.JsonSerializer.SerializeToElement(dto.Payload)
                    : default;
                return new SendMessageCommand(instanceName, dto.Operation, payload);
            });
    }
}

