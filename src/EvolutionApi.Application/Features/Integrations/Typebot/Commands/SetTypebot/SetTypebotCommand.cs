using EvolutionApi.Application.Common.Abstractions;
using EvolutionApi.Application.Features.Integrations.Typebot.Caching;
using EvolutionApi.Domain.Entities;
using EvolutionApi.Domain.Interfaces;

namespace EvolutionApi.Application.Features.Integrations.Typebot.Commands.SetTypebot;

// ── Command ───────────────────────────────────────────────────────────────────

public sealed record SetTypebotCommand(
    string  InstanceName,
    bool    Enabled         = false,
    string? Url             = null,
    string? TypebotId       = null,
    int?    Expire          = null,
    string? KeywordFinish   = null,
    int?    DelayMessage    = null,
    string? UnknownMessage  = null,
    bool?   ListeningFromMe = null) : ICommand<object>;

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class SetTypebotCommandHandler(
    IInstanceRepository instances,
    IIntegrationRepository integrations,
    ICacheService cache) : MediatR.IRequestHandler<SetTypebotCommand, object>
{
    public async Task<object> Handle(SetTypebotCommand cmd, CancellationToken ct)
    {
        var instance = await instances.GetByNameAsync(cmd.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{cmd.InstanceName}' not found.");

        var existing = await integrations.GetTypebotAsync(instance.Id, ct)
            ?? new TypebotSetting { InstanceId = instance.Id };

        existing.Enabled         = cmd.Enabled;
        existing.Url             = cmd.Url             ?? existing.Url;
        existing.TypebotId       = cmd.TypebotId       ?? existing.TypebotId;
        existing.Expire          = cmd.Expire          ?? existing.Expire;
        existing.KeywordFinish   = cmd.KeywordFinish   ?? existing.KeywordFinish;
        existing.DelayMessage    = cmd.DelayMessage    ?? existing.DelayMessage;
        existing.UnknownMessage  = cmd.UnknownMessage  ?? existing.UnknownMessage;
        existing.ListeningFromMe = cmd.ListeningFromMe ?? existing.ListeningFromMe;
        existing.UpdatedAt       = DateTime.UtcNow;

        await integrations.AddOrUpdateTypebotAsync(existing, ct);
        await cache.RemoveByTagAsync(TypebotCacheKeys.Tag(cmd.InstanceName), ct);

        return new { instanceName = cmd.InstanceName, typebot = cmd };
    }
}
