using EvolutionApi.Application.Common.Abstractions;
using EvolutionApi.Application.Features.Labels.Caching;
using EvolutionApi.Domain.Interfaces;
using FluentValidation;
using System.Text.Json;

namespace EvolutionApi.Application.Features.Labels.Commands.HandleLabel;

// ── Command ───────────────────────────────────────────────────────────────────

public sealed record HandleLabelCommand(
    string InstanceName,
    string Label,
    string ChatId,
    string Action = "add") : ICommand<JsonElement>;

// ── Validator ─────────────────────────────────────────────────────────────────

public sealed class HandleLabelCommandValidator : AbstractValidator<HandleLabelCommand>
{
    private static readonly string[] ValidActions = ["add", "remove"];

    public HandleLabelCommandValidator()
    {
        RuleFor(x => x.InstanceName).NotEmpty().WithMessage("instanceName is required");
        RuleFor(x => x.Label).NotEmpty().WithMessage("label is required");
        RuleFor(x => x.ChatId).NotEmpty().WithMessage("chatId is required");
        RuleFor(x => x.Action)
            .Must(a => ValidActions.Contains(a, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"action must be one of: {string.Join(", ", ValidActions)}");
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class HandleLabelCommandHandler(
    IInstanceRepository instances,
    IWhatsAppEngine engine,
    ICacheService cache) : MediatR.IRequestHandler<HandleLabelCommand, JsonElement>
{
    public async Task<JsonElement> Handle(HandleLabelCommand cmd, CancellationToken ct)
    {
        var instance = await instances.GetByNameAsync(cmd.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{cmd.InstanceName}' not found.");

        var result = await engine.HandleLabelAsync(instance.Id, cmd.Label, cmd.ChatId, cmd.Action, ct);

        await cache.RemoveByTagAsync(LabelCacheKeys.Tag(cmd.InstanceName), ct);

        return result;
    }
}
