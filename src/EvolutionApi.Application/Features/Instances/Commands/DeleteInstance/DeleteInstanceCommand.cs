using EvolutionApi.Application.Common.Abstractions;
using EvolutionApi.Application.Features.Instances.Caching;
using EvolutionApi.Domain.Interfaces;
using FluentValidation;

namespace EvolutionApi.Application.Features.Instances.Commands.DeleteInstance;

// ── Command ───────────────────────────────────────────────────────────────────

public sealed record DeleteInstanceCommand(string InstanceName) : ICommand<bool>;

// ── Validator ─────────────────────────────────────────────────────────────────

public sealed class DeleteInstanceCommandValidator : AbstractValidator<DeleteInstanceCommand>
{
    public DeleteInstanceCommandValidator()
    {
        RuleFor(x => x.InstanceName).NotEmpty().WithMessage("instanceName is required");
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class DeleteInstanceCommandHandler(
    IUnitOfWork uow,
    IWhatsAppEngine engine,
    ICacheService cache) : MediatR.IRequestHandler<DeleteInstanceCommand, bool>
{
    public async Task<bool> Handle(DeleteInstanceCommand cmd, CancellationToken ct)
    {
        var instance = await uow.Instances.GetByNameAsync(cmd.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{cmd.InstanceName}' not found.");

        try { await engine.StopAsync(instance.Id, ct); }
        catch { /* best-effort — continue with DB deletion even if engine is unreachable */ }

        uow.Instances.Remove(instance);
        await uow.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync(InstanceCacheKeys.Tag, ct);

        return true;
    }
}
