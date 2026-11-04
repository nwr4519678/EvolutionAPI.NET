using EvolutionApi.Application.Common.Abstractions;
using EvolutionApi.Domain.Entities;
using EvolutionApi.Domain.Interfaces;
using FluentValidation;

namespace EvolutionApi.Application.Features.Webhooks.Commands.SetWebhook;

// ── Command ───────────────────────────────────────────────────────────────────

/// <summary>
/// Creates or updates the webhook configuration for a given instance.
/// InstanceName is a required constructor parameter (not a mutable init property).
/// </summary>
public sealed record SetWebhookCommand(
    string InstanceName,
    bool   Enabled   = false,
    string Url       = "",
    string[]? Events  = null,
    Dictionary<string, string>? Headers = null,
    bool ByEvents = false,
    bool Base64   = false) : ICommand<WebhookConfigDto>;

// ── Response DTO ──────────────────────────────────────────────────────────────

public sealed record WebhookConfigDto(
    string InstanceId,
    bool   Enabled,
    string Url,
    string EventsJson,
    bool   ByEvents,
    bool   Base64);

// ── Validator ─────────────────────────────────────────────────────────────────

public sealed class SetWebhookCommandValidator : AbstractValidator<SetWebhookCommand>
{
    public SetWebhookCommandValidator()
    {
        RuleFor(x => x.InstanceName)
            .NotEmpty().WithMessage("instanceName is required");

        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("url is required")
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("url must be a valid absolute URL");
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class SetWebhookCommandHandler(
    IUnitOfWork uow,
    ICacheService cache) : MediatR.IRequestHandler<SetWebhookCommand, WebhookConfigDto>
{
    public async Task<WebhookConfigDto> Handle(SetWebhookCommand cmd, CancellationToken ct)
    {
        var instance = await uow.Instances.GetByNameAsync(cmd.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{cmd.InstanceName}' not found.");

        var existing = await uow.Webhooks.GetByInstanceIdAsync(instance.Id, ct);

        var eventsJson  = cmd.Events  is { Length: > 0 } ? System.Text.Json.JsonSerializer.Serialize(cmd.Events)  : "[]";
        var headersJson = cmd.Headers is { Count:  > 0 } ? System.Text.Json.JsonSerializer.Serialize(cmd.Headers) : "{}";

        if (existing is null)
        {
            existing = new WebhookConfig
            {
                InstanceId  = instance.Id,
                Enabled     = cmd.Enabled,
                Url         = cmd.Url,
                EventsJson  = eventsJson,
                HeadersJson = headersJson,
                ByEvents    = cmd.ByEvents,
                Base64      = cmd.Base64
            };
            await uow.Webhooks.AddAsync(existing, ct);
        }
        else
        {
            existing.Enabled     = cmd.Enabled;
            existing.Url         = cmd.Url;
            existing.EventsJson  = eventsJson;
            existing.HeadersJson = headersJson;
            existing.ByEvents    = cmd.ByEvents;
            existing.Base64      = cmd.Base64;
            existing.UpdatedAt   = DateTime.UtcNow;
            uow.Webhooks.Update(existing);
        }

        await uow.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync(
            EvolutionApi.Application.Features.Webhooks.Caching.WebhookCacheKeys.Tag(cmd.InstanceName), ct);

        return new WebhookConfigDto(
            existing.InstanceId,
            existing.Enabled,
            existing.Url,
            existing.EventsJson,
            existing.ByEvents,
            existing.Base64);
    }
}
