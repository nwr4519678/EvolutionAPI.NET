using EvolutionApi.Application.Common.Abstractions;
using EvolutionApi.Domain.Interfaces;
using FluentValidation;
using System.Text.Json;

namespace EvolutionApi.Application.Features.Chat.Commands.MarkMessageRead;

// ── Value Object ──────────────────────────────────────────────────────────────

public sealed record ReadMessageKey(string RemoteJid, string Id, string? Participant = null);

// ── Command ───────────────────────────────────────────────────────────────────

public sealed record MarkMessageReadCommand(
    string           InstanceName,
    ReadMessageKey[] ReadMessages) : ICommand<JsonElement>;

// ── Validator ─────────────────────────────────────────────────────────────────

public sealed class MarkMessageReadCommandValidator : AbstractValidator<MarkMessageReadCommand>
{
    public MarkMessageReadCommandValidator()
    {
        RuleFor(x => x.InstanceName).NotEmpty().WithMessage("instanceName is required");
        RuleFor(x => x.ReadMessages)
            .NotEmpty().WithMessage("readMessages must contain at least one item");
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class MarkMessageReadCommandHandler(
    IInstanceRepository instances,
    IWhatsAppEngine engine) : MediatR.IRequestHandler<MarkMessageReadCommand, JsonElement>
{
    public async Task<JsonElement> Handle(MarkMessageReadCommand cmd, CancellationToken ct)
    {
        var instance = await instances.GetByNameAsync(cmd.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{cmd.InstanceName}' not found.");

        var first = cmd.ReadMessages[0];
        var ids   = cmd.ReadMessages.Select(m => m.Id).ToArray();

        return await engine.MarkMessagesAsReadAsync(instance.Id, first.RemoteJid, ids, first.Participant, ct);
    }
}
