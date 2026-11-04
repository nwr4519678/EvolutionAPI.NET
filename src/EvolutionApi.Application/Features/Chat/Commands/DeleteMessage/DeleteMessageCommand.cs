using EvolutionApi.Application.Common.Abstractions;
using EvolutionApi.Domain.Interfaces;
using FluentValidation;

namespace EvolutionApi.Application.Features.Chat.Commands.DeleteMessage;

// ── Command ───────────────────────────────────────────────────────────────────

public sealed record DeleteMessageCommand(
    string  InstanceName,
    string  RemoteJid   = "",
    string  Id          = "",
    bool    FromMe      = true,
    string? Participant = null) : ICommand<DeleteMessageResult>;

// ── Response ──────────────────────────────────────────────────────────────────

public sealed record DeleteMessageResult(bool Deleted);

// ── Validator ─────────────────────────────────────────────────────────────────

public sealed class DeleteMessageCommandValidator : AbstractValidator<DeleteMessageCommand>
{
    public DeleteMessageCommandValidator()
    {
        RuleFor(x => x.InstanceName).NotEmpty().WithMessage("instanceName is required");
        RuleFor(x => x.RemoteJid).NotEmpty().WithMessage("remoteJid is required");
        RuleFor(x => x.Id).NotEmpty().WithMessage("id is required");
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class DeleteMessageCommandHandler(
    IInstanceRepository instances,
    IWhatsAppEngine engine) : MediatR.IRequestHandler<DeleteMessageCommand, DeleteMessageResult>
{
    public async Task<DeleteMessageResult> Handle(DeleteMessageCommand cmd, CancellationToken ct)
    {
        var instance = await instances.GetByNameAsync(cmd.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{cmd.InstanceName}' not found.");

        await engine.DeleteMessageForEveryoneAsync(
            instance.Id, cmd.RemoteJid, cmd.Id, cmd.FromMe, cmd.Participant, ct);

        return new DeleteMessageResult(true);
    }
}
