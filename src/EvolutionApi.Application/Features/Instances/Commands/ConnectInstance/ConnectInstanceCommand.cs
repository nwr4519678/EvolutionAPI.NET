using EvolutionApi.Application.Common.Abstractions;
using EvolutionApi.Application.Features.Instances.Caching;
using EvolutionApi.Domain.Interfaces;
using FluentValidation;
using System.Text.Json;

namespace EvolutionApi.Application.Features.Instances.Commands.ConnectInstance;

// ── Command ───────────────────────────────────────────────────────────────────

public sealed record ConnectInstanceCommand(
    string  InstanceName,
    string? Number = null) : ICommand<JsonElement>;

// ── Validator ─────────────────────────────────────────────────────────────────

public sealed class ConnectInstanceCommandValidator : AbstractValidator<ConnectInstanceCommand>
{
    public ConnectInstanceCommandValidator()
    {
        RuleFor(x => x.InstanceName).NotEmpty().WithMessage("instanceName is required");
        RuleFor(x => x.Number)
            .Matches(@"^\+?[1-9]\d{6,14}$").When(x => x.Number is not null)
            .WithMessage("number must be a valid international phone number");
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class ConnectInstanceCommandHandler(
    IUnitOfWork uow,
    IWhatsAppEngine engine,
    ICacheService cache) : MediatR.IRequestHandler<ConnectInstanceCommand, JsonElement>
{
    public async Task<JsonElement> Handle(ConnectInstanceCommand cmd, CancellationToken ct)
    {
        var instance = await uow.Instances.GetByNameAsync(cmd.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{cmd.InstanceName}' not found.");

        await cache.RemoveByTagAsync(InstanceCacheKeys.Tag, ct);

        return await engine.ConnectAsync(instance.Id, cmd.Number, ct);
    }
}
