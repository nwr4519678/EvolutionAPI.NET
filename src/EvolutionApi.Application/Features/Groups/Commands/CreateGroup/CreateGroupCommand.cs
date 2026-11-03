using EvolutionApi.Application.Common.Abstractions;
using EvolutionApi.Application.Features.Groups.Caching;
using EvolutionApi.Domain.Interfaces;
using FluentValidation;
using System.Text.Json;

namespace EvolutionApi.Application.Features.Groups.Commands.CreateGroup;

// ── Command ───────────────────────────────────────────────────────────────────

public sealed record CreateGroupCommand(
    string    InstanceName,
    string    Subject      = "",
    string[]? Participants = null) : ICommand<JsonElement>;

// ── Validator ─────────────────────────────────────────────────────────────────

public sealed class CreateGroupCommandValidator : AbstractValidator<CreateGroupCommand>
{
    public CreateGroupCommandValidator()
    {
        RuleFor(x => x.InstanceName).NotEmpty().WithMessage("instanceName is required");
        RuleFor(x => x.Subject).NotEmpty().WithMessage("subject is required");
        RuleFor(x => x.Participants).NotEmpty().WithMessage("participants must contain at least one item");
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class CreateGroupCommandHandler(
    IInstanceRepository instances,
    IWhatsAppEngine engine,
    ICacheService cache) : MediatR.IRequestHandler<CreateGroupCommand, JsonElement>
{
    public async Task<JsonElement> Handle(CreateGroupCommand cmd, CancellationToken ct)
    {
        var instance = await instances.GetByNameAsync(cmd.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{cmd.InstanceName}' not found.");

        var jids = (cmd.Participants ?? [])
            .Select(p => p.Contains('@') ? p : $"{p.TrimStart('+')}@s.whatsapp.net")
            .ToArray();

        var result = await engine.CreateGroupAsync(instance.Id, cmd.Subject, jids, ct);
        await cache.RemoveByTagAsync(GroupCacheKeys.Tag(cmd.InstanceName), ct);
        return result;
    }
}
