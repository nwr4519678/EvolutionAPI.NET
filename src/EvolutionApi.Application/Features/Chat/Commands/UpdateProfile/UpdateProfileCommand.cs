using EvolutionApi.Application.Common.Abstractions;
using EvolutionApi.Domain.Interfaces;
using FluentValidation;
using MediatR;
using System.Text.Json;

namespace EvolutionApi.Application.Features.Chat.Commands.UpdateProfile;

// ── Update Profile Name ───────────────────────────────────────────────────────

public sealed record UpdateProfileNameCommand(string InstanceName, string Name) : ICommand<JsonElement>;

public sealed class UpdateProfileNameCommandValidator : AbstractValidator<UpdateProfileNameCommand>
{
    public UpdateProfileNameCommandValidator()
    {
        RuleFor(x => x.InstanceName).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().WithMessage("name is required");
    }
}

public sealed class UpdateProfileNameCommandHandler(
    IInstanceRepository instances,
    IWhatsAppEngine engine,
    ICacheService cache) : IRequestHandler<UpdateProfileNameCommand, JsonElement>
{
    public async Task<JsonElement> Handle(UpdateProfileNameCommand cmd, CancellationToken ct)
    {
        var instance = await instances.GetByNameAsync(cmd.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{cmd.InstanceName}' not found.");
        await engine.UpdateProfileNameAsync(instance.Id, cmd.Name, ct);
        await cache.RemoveByTagAsync(EvolutionApi.Application.Features.Chat.Caching.ChatCacheKeys.Tag(cmd.InstanceName), ct);
        return JsonSerializer.SerializeToElement(new { name = cmd.Name }, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}

// ── Update Profile Status ─────────────────────────────────────────────────────

public sealed record UpdateProfileStatusCommand(string InstanceName, string Status) : ICommand<JsonElement>;

public sealed class UpdateProfileStatusCommandValidator : AbstractValidator<UpdateProfileStatusCommand>
{
    public UpdateProfileStatusCommandValidator()
    {
        RuleFor(x => x.InstanceName).NotEmpty();
        RuleFor(x => x.Status).NotEmpty().WithMessage("status is required");
    }
}

public sealed class UpdateProfileStatusCommandHandler(
    IInstanceRepository instances,
    IWhatsAppEngine engine,
    ICacheService cache) : IRequestHandler<UpdateProfileStatusCommand, JsonElement>
{
    public async Task<JsonElement> Handle(UpdateProfileStatusCommand cmd, CancellationToken ct)
    {
        var instance = await instances.GetByNameAsync(cmd.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{cmd.InstanceName}' not found.");
        await engine.UpdateProfileStatusAsync(instance.Id, cmd.Status, ct);
        await cache.RemoveByTagAsync(EvolutionApi.Application.Features.Chat.Caching.ChatCacheKeys.Tag(cmd.InstanceName), ct);
        return JsonSerializer.SerializeToElement(new { status = cmd.Status }, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}

// ── Update Block Status ───────────────────────────────────────────────────────

public sealed record UpdateBlockStatusCommand(string InstanceName, string Number, string Status) : ICommand<JsonElement>;

public sealed class UpdateBlockStatusCommandValidator : AbstractValidator<UpdateBlockStatusCommand>
{
    public UpdateBlockStatusCommandValidator()
    {
        RuleFor(x => x.InstanceName).NotEmpty();
        RuleFor(x => x.Number).NotEmpty().WithMessage("number is required");
        RuleFor(x => x.Status).Must(s => s is "block" or "unblock").WithMessage("status must be block or unblock");
    }
}

public sealed class UpdateBlockStatusCommandHandler(
    IInstanceRepository instances,
    IWhatsAppEngine engine) : IRequestHandler<UpdateBlockStatusCommand, JsonElement>
{
    public async Task<JsonElement> Handle(UpdateBlockStatusCommand cmd, CancellationToken ct)
    {
        var instance = await instances.GetByNameAsync(cmd.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{cmd.InstanceName}' not found.");
        var jid = $"{new string(cmd.Number.Where(char.IsDigit).ToArray())}@s.whatsapp.net";
        await engine.UpdateBlockStatusAsync(instance.Id, jid, cmd.Status, ct);
        return JsonSerializer.SerializeToElement(new { jid, status = cmd.Status }, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}
