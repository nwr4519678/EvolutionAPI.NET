using EvolutionApi.Application.Common.Abstractions;
using EvolutionApi.Application.Features.Instances.Caching;
using EvolutionApi.Domain.Interfaces;
using FluentValidation;

namespace EvolutionApi.Application.Features.Instances.Commands.DisconnectInstance;

// ── Command ───────────────────────────────────────────────────────────────────

public sealed record DisconnectInstanceCommand(string InstanceName) : ICommand<bool>;

// ── Validator ─────────────────────────────────────────────────────────────────

public sealed class DisconnectInstanceCommandValidator : AbstractValidator<DisconnectInstanceCommand>
{
    public DisconnectInstanceCommandValidator()
    {
        RuleFor(x => x.InstanceName).NotEmpty().WithMessage("instanceName is required");
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class DisconnectInstanceCommandHandler(
    IUnitOfWork uow,
    IWhatsAppEngine engine,
    ICacheService cache) : MediatR.IRequestHandler<DisconnectInstanceCommand, bool>
{
    public async Task<bool> Handle(DisconnectInstanceCommand cmd, CancellationToken ct)
    {
        var instance = await uow.Instances.GetByNameAsync(cmd.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{cmd.InstanceName}' not found.");

        await engine.StopAsync(instance.Id, ct);

        instance.ConnectionStatus = "close";
        instance.UpdatedAt        = DateTime.UtcNow;
        uow.Instances.Update(instance);
        await uow.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync(InstanceCacheKeys.Tag, ct);

        return true;
    }
}
