using EvolutionApi.Application.Common.Abstractions;
using EvolutionApi.Domain.Interfaces;

namespace EvolutionApi.Application.Features.Integrations.Typebot.Commands.TypebotChangeStatus;

// ── Command ───────────────────────────────────────────────────────────────────

public sealed record TypebotChangeStatusCommand(
    string  InstanceName,
    string  RemoteJid = "",
    string? Status    = null) : ICommand<object>;

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class TypebotChangeStatusCommandHandler(
    IInstanceRepository instances,
    IIntegrationRepository integrations) : MediatR.IRequestHandler<TypebotChangeStatusCommand, object>
{
    public async Task<object> Handle(TypebotChangeStatusCommand cmd, CancellationToken ct)
    {
        var instance = await instances.GetByNameAsync(cmd.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{cmd.InstanceName}' not found.");

        var session = await integrations.GetTypebotSessionAsync(instance.Id, cmd.RemoteJid, ct)
            ?? throw new KeyNotFoundException($"No active Typebot session for remoteJid '{cmd.RemoteJid}'.");

        session.Status    = cmd.Status?.ToLowerInvariant() == "closed" ? "closed" : "paused";
        session.UpdatedAt = DateTime.UtcNow;

        await integrations.AddOrUpdateTypebotSessionAsync(session, ct);

        return new { instanceName = cmd.InstanceName, remoteJid = cmd.RemoteJid, status = session.Status };
    }
}
