using EvolutionApi.Application.Common.Abstractions;
using EvolutionApi.Application.Features.Labels.Caching;
using EvolutionApi.Domain.Interfaces;
using FluentValidation;
using System.Text.Json;

namespace EvolutionApi.Application.Features.Labels.Commands.CreateLabel;

// ── Command ───────────────────────────────────────────────────────────────────

public sealed record CreateLabelCommand(
    string InstanceName,
    string Name  = "",
    string Color = "#2196F3") : ICommand<JsonElement>;

// ── Validator ─────────────────────────────────────────────────────────────────

public sealed class CreateLabelCommandValidator : AbstractValidator<CreateLabelCommand>
{
    public CreateLabelCommandValidator()
    {
        RuleFor(x => x.InstanceName).NotEmpty().WithMessage("instanceName is required");
        RuleFor(x => x.Name).NotEmpty().WithMessage("name is required");
        RuleFor(x => x.Color)
            .Matches(@"^#([0-9A-Fa-f]{3}|[0-9A-Fa-f]{6})$")
            .When(x => !string.IsNullOrEmpty(x.Color))
            .WithMessage("color must be a valid hex color code (e.g. #2196F3)");
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class CreateLabelCommandHandler(
    IInstanceRepository instances,
    IWhatsAppEngine engine,
    ICacheService cache) : MediatR.IRequestHandler<CreateLabelCommand, JsonElement>
{
    public async Task<JsonElement> Handle(CreateLabelCommand cmd, CancellationToken ct)
    {
        var instance = await instances.GetByNameAsync(cmd.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{cmd.InstanceName}' not found.");

        var result = await engine.CreateLabelAsync(instance.Id, cmd.Name, cmd.Color, ct);

        await cache.RemoveByTagAsync(LabelCacheKeys.Tag(cmd.InstanceName), ct);

        return result;
    }
}
