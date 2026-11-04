using EvolutionApi.Application.Common.Abstractions;
using EvolutionApi.Domain.Interfaces;
using FluentValidation;
using MediatR;
using System.Text.Json;

namespace EvolutionApi.Application.Features.Groups.Commands.LeaveGroup;

public sealed record LeaveGroupCommand(string InstanceName, string GroupJid) : ICommand<JsonElement>;

public sealed class LeaveGroupCommandValidator : AbstractValidator<LeaveGroupCommand>
{
    public LeaveGroupCommandValidator()
    {
        RuleFor(x => x.InstanceName).NotEmpty();
        RuleFor(x => x.GroupJid).NotEmpty().WithMessage("groupJid is required");
    }
}

public sealed class LeaveGroupCommandHandler(
    IInstanceRepository instances,
    IWhatsAppEngine engine,
    ICacheService cache) : IRequestHandler<LeaveGroupCommand, JsonElement>
{
    public async Task<JsonElement> Handle(LeaveGroupCommand cmd, CancellationToken ct)
    {
        var instance = await instances.GetByNameAsync(cmd.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{cmd.InstanceName}' not found.");
        var jid = cmd.GroupJid.Contains('@') ? cmd.GroupJid : $"{cmd.GroupJid}@g.us";
        await engine.LeaveGroupAsync(instance.Id, jid, ct);
        await cache.RemoveByTagAsync(EvolutionApi.Application.Features.Groups.Caching.GroupCacheKeys.Tag(cmd.InstanceName), ct);
        return JsonSerializer.SerializeToElement(new { left = true, groupJid = cmd.GroupJid }, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}
