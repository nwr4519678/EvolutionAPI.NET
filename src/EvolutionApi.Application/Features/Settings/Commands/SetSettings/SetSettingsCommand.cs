using EvolutionApi.Application.Common.Abstractions;
using EvolutionApi.Application.Features.Settings.Caching;
using EvolutionApi.Domain.Entities;
using EvolutionApi.Domain.Interfaces;
using FluentValidation;

namespace EvolutionApi.Application.Features.Settings.Commands.SetSettings;

// ── Command ───────────────────────────────────────────────────────────────────

public sealed record SetSettingsCommand(
    string  InstanceName,
    bool?   RejectCall      = null,
    string? MsgCall         = null,
    bool?   GroupsIgnore    = null,
    bool?   AlwaysOnline    = null,
    bool?   ReadMessages    = null,
    bool?   ReadStatus      = null,
    bool?   SyncFullHistory = null,
    string? WavoipToken     = null) : ICommand<SetSettingsResult>;

// ── Response DTO ──────────────────────────────────────────────────────────────

public sealed record SetSettingsResult(string InstanceName, string Status);

// ── Validator ─────────────────────────────────────────────────────────────────

public sealed class SetSettingsCommandValidator : AbstractValidator<SetSettingsCommand>
{
    public SetSettingsCommandValidator()
    {
        RuleFor(x => x.InstanceName).NotEmpty().WithMessage("instanceName is required");
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class SetSettingsCommandHandler(
    IInstanceRepository instances,
    ISettingsRepository settings,
    ICacheService cache) : MediatR.IRequestHandler<SetSettingsCommand, SetSettingsResult>
{
    public async Task<SetSettingsResult> Handle(SetSettingsCommand cmd, CancellationToken ct)
    {
        var instance = await instances.GetByNameAsync(cmd.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{cmd.InstanceName}' not found.");

        var existing = await settings.GetSettingsAsync(instance.Id, ct)
            ?? new InstanceSettings { InstanceId = instance.Id };

        // Only apply fields that were explicitly provided (null = keep existing)
        if (cmd.RejectCall.HasValue)      existing.RejectCall      = cmd.RejectCall.Value;
        if (cmd.MsgCall is not null)      existing.MsgCall         = cmd.MsgCall;
        if (cmd.GroupsIgnore.HasValue)    existing.GroupsIgnore    = cmd.GroupsIgnore.Value;
        if (cmd.AlwaysOnline.HasValue)    existing.AlwaysOnline    = cmd.AlwaysOnline.Value;
        if (cmd.ReadMessages.HasValue)    existing.ReadMessages    = cmd.ReadMessages.Value;
        if (cmd.ReadStatus.HasValue)      existing.ReadStatus      = cmd.ReadStatus.Value;
        if (cmd.SyncFullHistory.HasValue) existing.SyncFullHistory = cmd.SyncFullHistory.Value;
        if (cmd.WavoipToken is not null)  existing.WavoipToken     = cmd.WavoipToken;

        await settings.AddOrUpdateSettingsAsync(existing, ct);
        await cache.RemoveByTagAsync(SettingsCacheKeys.Tag(cmd.InstanceName), ct);

        return new SetSettingsResult(cmd.InstanceName, "updated");
    }
}
