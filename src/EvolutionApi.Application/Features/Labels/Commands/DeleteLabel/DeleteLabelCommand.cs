using EvolutionApi.Application.Common.Abstractions;
using EvolutionApi.Application.Features.Labels.Caching;
using EvolutionApi.Domain.Interfaces;
using FluentValidation;
using System.Text.Json;

namespace EvolutionApi.Application.Features.Labels.Commands.DeleteLabel;

// ── Command ───────────────────────────────────────────────────────────────────

public sealed record DeleteLabelCommand(
    string InstanceName,
    string LabelId) : ICommand<JsonElement>;

// ── Validator ─────────────────────────────────────────────────────────────────

public sealed class DeleteLabelCommandValidator : AbstractValidator<DeleteLabelCommand>
{
    public DeleteLabelCommandValidator()
    {
        RuleFor(x => x.InstanceName).NotEmpty().WithMessage("instanceName is required");
        RuleFor(x => x.LabelId).NotEmpty().WithMessage("labelId is required");
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class DeleteLabelCommandHandler(
    IInstanceRepository instances,
    IWhatsAppEngine engine,
    ICacheService cache) : MediatR.IRequestHandler<DeleteLabelCommand, JsonElement>
{
    public async Task<JsonElement> Handle(DeleteLabelCommand cmd, CancellationToken ct)
    {
        var instance = await instances.GetByNameAsync(cmd.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{cmd.InstanceName}' not found.");

        var result = await engine.DeleteLabelAsync(instance.Id, cmd.LabelId, ct);

        await cache.RemoveByTagAsync(LabelCacheKeys.Tag(cmd.InstanceName), ct);

        return result;
    }
}
