using EvolutionApi.Application.Common.Interfaces;
using EvolutionApi.Domain.Interfaces;
using FluentValidation;
using MediatR;
using System.Text.Json;

namespace EvolutionApi.Application.Features.Messages.Commands.SendMessage;

// ── Command ───────────────────────────────────────────────────────────────────

public sealed record SendMessageCommand(
    string InstanceName,
    string Operation,    // e.g. "sendText", "sendMedia", "sendAudio"
    JsonElement Payload) : IRequest<JsonElement>;

// ── Validator ─────────────────────────────────────────────────────────────────

public sealed class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    private static readonly HashSet<string> ValidOps = new(StringComparer.OrdinalIgnoreCase)
    {
        "sendText","sendStatus","sendMedia","sendWhatsAppAudio","sendPtv","sendSticker",
        "sendLocation","sendContact","sendReaction","sendPoll","sendList",
        "sendButtons","sendTemplate"
    };

    public SendMessageCommandValidator()
    {
        RuleFor(x => x.InstanceName).NotEmpty().WithMessage("instanceName is required");
        RuleFor(x => x.Operation)
            .NotEmpty()
            .Must(op => ValidOps.Contains(op))
            .WithMessage(x => $"'{x.Operation}' is not a valid message operation. Valid: {string.Join(", ", ValidOps)}");
        RuleFor(x => x.Payload)
            .Must(p => p.ValueKind == JsonValueKind.Object)
            .WithMessage("payload must be a JSON object");
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class SendMessageCommandHandler(
    IUnitOfWork uow,
    IThrottledMessageQueue throttler) : IRequestHandler<SendMessageCommand, JsonElement>
{
    public async Task<JsonElement> Handle(SendMessageCommand cmd, CancellationToken ct)
    {
        var instance = await uow.Instances.GetByNameAsync(cmd.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{cmd.InstanceName}' not found.");

        if (instance.ConnectionStatus != "open")
            throw new InvalidOperationException(
                $"Instance '{cmd.InstanceName}' is not connected (status: {instance.ConnectionStatus}).");

        var tenantId = instance.TenantId ?? "default";

        // Route through the per-tenant channel throttling queue (safely spaced 3-6s apart)
        return await throttler.EnqueueAndAwaitAsync(
            tenantId,
            instance.Id,
            instance.Name,
            cmd.Operation,
            cmd.Payload,
            ct);
    }
}
