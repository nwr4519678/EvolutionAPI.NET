using EvolutionApi.Application.Common.Abstractions;
using EvolutionApi.Application.Features.Groups.Caching;
using EvolutionApi.Domain.Interfaces;
using FluentValidation;
using MediatR;
using System.Text.Json;

namespace EvolutionApi.Application.Features.Groups.Commands.UpdateGroupSubject;

public sealed record UpdateGroupSubjectCommand(
    string InstanceName,
    string GroupJid = "",
    string Subject  = "") : ICommand<JsonElement>;

public sealed class UpdateGroupSubjectCommandValidator : AbstractValidator<UpdateGroupSubjectCommand>
{
    public UpdateGroupSubjectCommandValidator()
    {
        RuleFor(x => x.InstanceName).NotEmpty().WithMessage("instanceName is required");
        RuleFor(x => x.GroupJid).NotEmpty().WithMessage("groupJid is required");
        RuleFor(x => x.Subject).NotEmpty().WithMessage("subject is required");
    }
}

public sealed class UpdateGroupSubjectCommandHandler(
    IInstanceRepository instances,
    IWhatsAppEngine engine,
    ICacheService cache) : IRequestHandler<UpdateGroupSubjectCommand, JsonElement>
{
    public async Task<JsonElement> Handle(UpdateGroupSubjectCommand cmd, CancellationToken ct)
    {
        var instance = await instances.GetByNameAsync(cmd.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{cmd.InstanceName}' not found.");
        var jid = cmd.GroupJid.Contains('@') ? cmd.GroupJid : $"{cmd.GroupJid}@g.us";
        await engine.UpdateGroupSubjectAsync(instance.Id, jid, cmd.Subject, ct);
        await cache.RemoveByTagAsync(GroupCacheKeys.Tag(cmd.InstanceName), ct);
        return JsonSerializer.SerializeToElement(new { updated = true }, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}
