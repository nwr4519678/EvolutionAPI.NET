using EvolutionApi.Application.Common.Abstractions;
using EvolutionApi.Domain.Entities;
using EvolutionApi.Domain.Interfaces;
using FluentValidation;

namespace EvolutionApi.Application.Features.Integrations.Typebot.Commands.TypebotStart;

// ── Command ───────────────────────────────────────────────────────────────────

public sealed record TypebotStartCommand(
    string  InstanceName,
    string  RemoteJid = "",
    string? Url       = null) : ICommand<object>;

// ── Validator ─────────────────────────────────────────────────────────────────

public sealed class TypebotStartCommandValidator : AbstractValidator<TypebotStartCommand>
{
    public TypebotStartCommandValidator()
    {
        RuleFor(x => x.InstanceName).NotEmpty().WithMessage("instanceName is required");
        RuleFor(x => x.RemoteJid).NotEmpty().WithMessage("remoteJid is required");
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class TypebotStartCommandHandler(
    IInstanceRepository instances,
    IIntegrationRepository integrations) : MediatR.IRequestHandler<TypebotStartCommand, object>
{
    public async Task<object> Handle(TypebotStartCommand cmd, CancellationToken ct)
    {
        var instance = await instances.GetByNameAsync(cmd.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{cmd.InstanceName}' not found.");

        var existing = await integrations.GetTypebotSessionAsync(instance.Id, cmd.RemoteJid, ct)
            ?? new TypebotSession { InstanceId = instance.Id, RemoteJid = cmd.RemoteJid };

        var sessionId      = Guid.NewGuid().ToString("N");
        existing.SessionId = sessionId;
        existing.Status    = "opened";
        existing.UpdatedAt = DateTime.UtcNow;

        await integrations.AddOrUpdateTypebotSessionAsync(existing, ct);

        return new { instanceName = cmd.InstanceName, remoteJid = cmd.RemoteJid, sessionId, status = "opened" };
    }
}
