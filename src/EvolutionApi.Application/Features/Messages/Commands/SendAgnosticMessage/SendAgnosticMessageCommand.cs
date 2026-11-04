using System.Text.Json;
using EvolutionApi.Application.Common.Interfaces;
using EvolutionApi.Domain.Enums;
using EvolutionApi.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace EvolutionApi.Application.Features.Messages.Commands.SendAgnosticMessage;

// ── Command ───────────────────────────────────────────────────────────────────

public sealed record SendAgnosticMessageCommand(
    string InstanceName,
    string DestinationPhone,
    AgnosticMessageType MessageType,
    string? Text = null,
    string? MediaUrlOrBase64 = null,
    string? Caption = null,
    string? FileName = null,
    string? MimeType = null,
    double? Latitude = null,
    double? Longitude = null,
    string? ContactName = null,
    string? ContactPhone = null) : IRequest<JsonElement>, ITenantScopedRequest
{
    public string? TenantId { get; set; }
}

// ── Validator ─────────────────────────────────────────────────────────────────

public sealed class SendAgnosticMessageCommandValidator : AbstractValidator<SendAgnosticMessageCommand>
{
    public SendAgnosticMessageCommandValidator()
    {
        RuleFor(x => x.InstanceName).NotEmpty().WithMessage("instanceName is required");
        RuleFor(x => x.DestinationPhone).NotEmpty().WithMessage("destinationPhone is required");
        RuleFor(x => x.MessageType).IsInEnum().WithMessage("Valid messageType is required");

        RuleFor(x => x.Text)
            .NotEmpty()
            .When(x => x.MessageType == AgnosticMessageType.Text)
            .WithMessage("text is required for Text message type");

        RuleFor(x => x.MediaUrlOrBase64)
            .NotEmpty()
            .When(x => x.MessageType is AgnosticMessageType.MediaImage or AgnosticMessageType.MediaVideo or AgnosticMessageType.MediaAudio or AgnosticMessageType.MediaDocument)
            .WithMessage("mediaUrlOrBase64 is required for media message types");
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class SendAgnosticMessageCommandHandler(
    IUnitOfWork uow,
    IThrottledMessageQueue throttler) : IRequestHandler<SendAgnosticMessageCommand, JsonElement>
{
    public async Task<JsonElement> Handle(SendAgnosticMessageCommand cmd, CancellationToken ct)
    {
        var instance = await uow.Instances.GetByNameAsync(cmd.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{cmd.InstanceName}' not found.");

        if (instance.ConnectionStatus != "open")
            throw new InvalidOperationException($"Instance '{cmd.InstanceName}' is not connected (status: {instance.ConnectionStatus}).");

        var tenantId = cmd.TenantId ?? instance.TenantId ?? "default";
        var (operation, payload) = BuildOperationAndPayload(cmd);

        // Dispatches through the per-tenant Channel throttler (enforcing 3-6 second safety jitter)
        return await throttler.EnqueueAndAwaitAsync(
            tenantId,
            instance.Id,
            instance.Name,
            operation,
            payload,
            ct);
    }

    private static (string Operation, JsonElement Payload) BuildOperationAndPayload(SendAgnosticMessageCommand cmd)
    {
        var serializerOpts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        return cmd.MessageType switch
        {
            AgnosticMessageType.Text => (
                "sendText",
                JsonSerializer.SerializeToElement(new
                {
                    number = cmd.DestinationPhone,
                    text = new { message = cmd.Text ?? string.Empty }
                }, serializerOpts)),

            AgnosticMessageType.MediaImage => (
                "sendMedia",
                JsonSerializer.SerializeToElement(new
                {
                    number = cmd.DestinationPhone,
                    mediatype = "image",
                    media = cmd.MediaUrlOrBase64,
                    caption = cmd.Caption,
                    fileName = cmd.FileName ?? "image.jpg",
                    mimeType = cmd.MimeType ?? "image/jpeg"
                }, serializerOpts)),

            AgnosticMessageType.MediaVideo => (
                "sendMedia",
                JsonSerializer.SerializeToElement(new
                {
                    number = cmd.DestinationPhone,
                    mediatype = "video",
                    media = cmd.MediaUrlOrBase64,
                    caption = cmd.Caption,
                    fileName = cmd.FileName ?? "video.mp4",
                    mimeType = cmd.MimeType ?? "video/mp4"
                }, serializerOpts)),

            AgnosticMessageType.MediaAudio => (
                "sendWhatsAppAudio",
                JsonSerializer.SerializeToElement(new
                {
                    number = cmd.DestinationPhone,
                    audio = cmd.MediaUrlOrBase64,
                    mimetype = cmd.MimeType ?? "audio/ogg; codecs=opus"
                }, serializerOpts)),

            AgnosticMessageType.MediaDocument => (
                "sendMedia",
                JsonSerializer.SerializeToElement(new
                {
                    number = cmd.DestinationPhone,
                    mediatype = "document",
                    media = cmd.MediaUrlOrBase64,
                    caption = cmd.Caption,
                    fileName = cmd.FileName ?? "document.pdf",
                    mimeType = cmd.MimeType ?? "application/pdf"
                }, serializerOpts)),

            AgnosticMessageType.Location => (
                "sendLocation",
                JsonSerializer.SerializeToElement(new
                {
                    number = cmd.DestinationPhone,
                    latitude = cmd.Latitude ?? 0,
                    longitude = cmd.Longitude ?? 0,
                    name = cmd.Caption,
                    address = cmd.Text
                }, serializerOpts)),

            AgnosticMessageType.Contact => (
                "sendContact",
                JsonSerializer.SerializeToElement(new
                {
                    number = cmd.DestinationPhone,
                    contact = new[]
                    {
                        new { fullName = cmd.ContactName ?? "Contact", waNumber = cmd.ContactPhone ?? cmd.DestinationPhone }
                    }
                }, serializerOpts)),

            _ => throw new NotSupportedException($"Agnostic message type {cmd.MessageType} is not supported.")
        };
    }
}
