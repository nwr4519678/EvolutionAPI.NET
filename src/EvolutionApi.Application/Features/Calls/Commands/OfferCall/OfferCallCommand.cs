using EvolutionApi.Application.Common.Abstractions;
using EvolutionApi.Domain.Interfaces;
using FluentValidation;
using System.Text.Json;

namespace EvolutionApi.Application.Features.Calls.Commands.OfferCall;

// ── Command ───────────────────────────────────────────────────────────────────

public sealed record OfferCallCommand(
    string      InstanceName,
    JsonElement Payload = default) : ICommand<JsonElement>;

// ── Validator ─────────────────────────────────────────────────────────────────

public sealed class OfferCallCommandValidator : AbstractValidator<OfferCallCommand>
{
    public OfferCallCommandValidator()
    {
        RuleFor(x => x.InstanceName).NotEmpty().WithMessage("instanceName is required");
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class OfferCallCommandHandler(
    IInstanceRepository instances,
    IWhatsAppEngine engine) : MediatR.IRequestHandler<OfferCallCommand, JsonElement>
{
    public async Task<JsonElement> Handle(OfferCallCommand cmd, CancellationToken ct)
    {
        var instance = await instances.GetByNameAsync(cmd.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{cmd.InstanceName}' not found.");

        return await engine.OfferCallAsync(instance.Id, cmd.Payload, ct);
    }
}
