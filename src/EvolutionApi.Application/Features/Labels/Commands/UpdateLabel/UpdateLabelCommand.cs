using EvolutionApi.Application.Common.Abstractions;
using EvolutionApi.Application.Features.Labels.Caching;
using EvolutionApi.Domain.Interfaces;
using FluentValidation;
using System.Text.Json;

namespace EvolutionApi.Application.Features.Labels.Commands.UpdateLabel;

// ── Command ───────────────────────────────────────────────────────────────────

public sealed record UpdateLabelCommand(
    string  InstanceName,
    string  LabelId,
    string? Name  = null,
    string? Color = null) : ICommand<JsonElement>;

// ── Validator ─────────────────────────────────────────────────────────────────

public sealed class UpdateLabelCommandValidator : AbstractValidator<UpdateLabelCommand>
{
    public UpdateLabelCommandValidator()
    {
        RuleFor(x => x.InstanceName).NotEmpty().WithMessage("instanceName is required");
        RuleFor(x => x.LabelId).NotEmpty().WithMessage("labelId is required");
        RuleFor(x => x.Color)
            .Matches(@"^#([0-9A-Fa-f]{3}|[0-9A-Fa-f]{6})$")
            .When(x => x.Color is not null)
            .WithMessage("color must be a valid hex color code");
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class UpdateLabelCommandHandler(
    IInstanceRepository instances,
    IWhatsAppEngine engine,
    ICacheService cache) : MediatR.IRequestHandler<UpdateLabelCommand, JsonElement>
{
    public async Task<JsonElement> Handle(UpdateLabelCommand cmd, CancellationToken ct)
    {
        var instance = await instances.GetByNameAsync(cmd.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{cmd.InstanceName}' not found.");

        var result = await engine.UpdateLabelAsync(instance.Id, cmd.LabelId, cmd.Name, cmd.Color, ct);

        await cache.RemoveByTagAsync(LabelCacheKeys.Tag(cmd.InstanceName), ct);

        return result;
    }
}
