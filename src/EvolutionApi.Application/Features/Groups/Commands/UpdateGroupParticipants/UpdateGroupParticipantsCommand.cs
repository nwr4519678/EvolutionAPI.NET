using EvolutionApi.Application.Common.Abstractions;
using EvolutionApi.Application.Features.Groups.Caching;
using EvolutionApi.Domain.Interfaces;
using FluentValidation;
using System.Text.Json;

namespace EvolutionApi.Application.Features.Groups.Commands.UpdateGroupParticipants;

// ── Command ───────────────────────────────────────────────────────────────────

public sealed record UpdateGroupParticipantsCommand(
    string    InstanceName,
    string    GroupJid     = "",
    string[]? Participants = null,
    string    Action       = "") : ICommand<JsonElement>;

// ── Validator ─────────────────────────────────────────────────────────────────

public sealed class UpdateGroupParticipantsCommandValidator : AbstractValidator<UpdateGroupParticipantsCommand>
{
    private static readonly string[] ValidActions = ["add", "remove", "promote", "demote"];

    public UpdateGroupParticipantsCommandValidator()
    {
        RuleFor(x => x.InstanceName).NotEmpty().WithMessage("instanceName is required");
        RuleFor(x => x.GroupJid).NotEmpty().WithMessage("groupJid is required");
        RuleFor(x => x.Participants).NotEmpty().WithMessage("participants must contain at least one item");
        RuleFor(x => x.Action)
            .Must(a => ValidActions.Contains(a, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"action must be one of: {string.Join(", ", ValidActions)}");
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class UpdateGroupParticipantsCommandHandler(
    IInstanceRepository instances,
    IWhatsAppEngine engine,
    ICacheService cache) : MediatR.IRequestHandler<UpdateGroupParticipantsCommand, JsonElement>
{
    public async Task<JsonElement> Handle(UpdateGroupParticipantsCommand cmd, CancellationToken ct)
    {
        var instance = await instances.GetByNameAsync(cmd.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{cmd.InstanceName}' not found.");

        var jids = (cmd.Participants ?? [])
            .Select(p => p.Contains('@') ? p : $"{p.TrimStart('+')}@s.whatsapp.net")
            .ToArray();

        var groupJid = cmd.GroupJid.Contains('@') ? cmd.GroupJid : $"{cmd.GroupJid}@g.us";

        var result = await engine.UpdateGroupParticipantsAsync(instance.Id, groupJid, jids, cmd.Action, ct);
        await cache.RemoveByTagAsync(GroupCacheKeys.Tag(cmd.InstanceName), ct);
        return result;
    }
}
