using EvolutionApi.Application.Common.Abstractions;
using EvolutionApi.Domain.Entities;
using EvolutionApi.Domain.Interfaces;
using FluentValidation;

namespace EvolutionApi.Application.Features.Integrations.OpenAi.Commands.StartOpenAi;

// ── Command ───────────────────────────────────────────────────────────────────

public sealed record StartOpenAiCommand(
    string  InstanceName,
    string  RemoteJid = "") : ICommand<object>;

// ── Validator ─────────────────────────────────────────────────────────────────

public sealed class StartOpenAiCommandValidator : AbstractValidator<StartOpenAiCommand>
{
    public StartOpenAiCommandValidator()
    {
        RuleFor(x => x.InstanceName).NotEmpty().WithMessage("instanceName is required");
        RuleFor(x => x.RemoteJid).NotEmpty().WithMessage("remoteJid is required");
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class StartOpenAiCommandHandler(
    IInstanceRepository instances,
    IIntegrationRepository integrations) : MediatR.IRequestHandler<StartOpenAiCommand, object>
{
    public async Task<object> Handle(StartOpenAiCommand cmd, CancellationToken ct)
    {
        var instance = await instances.GetByNameAsync(cmd.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{cmd.InstanceName}' not found.");

        var existing = await integrations.GetOpenAiSessionAsync(instance.Id, cmd.RemoteJid, ct)
            ?? new OpenAiSession { InstanceId = instance.Id, RemoteJid = cmd.RemoteJid };

        existing.Status    = "opened";
        existing.UpdatedAt = DateTime.UtcNow;

        await integrations.AddOrUpdateOpenAiSessionAsync(existing, ct);

        return new { instanceName = cmd.InstanceName, remoteJid = cmd.RemoteJid, status = "opened" };
    }
}
