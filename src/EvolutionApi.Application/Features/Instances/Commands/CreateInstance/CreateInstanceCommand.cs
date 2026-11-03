using EvolutionApi.Application.Common.Abstractions;
using EvolutionApi.Application.Common.Interfaces;
using EvolutionApi.Domain.Entities;
using EvolutionApi.Domain.Interfaces;
using FluentValidation;
using MediatR;
using System.Security.Cryptography;
using System.Text;

namespace EvolutionApi.Application.Features.Instances.Commands.CreateInstance;

// ── Command ───────────────────────────────────────────────────────────────────

public sealed record CreateInstanceCommand(
    string InstanceName,
    string Token,
    string? Number = null,
    string? WebhookUrl = null,
    string[]? WebhookEvents = null,
    bool RejectCall = false,
    bool GroupsIgnore = false,
    bool AlwaysOnline = false,
    bool ReadMessages = false) : ICommand<CreateInstanceResult>, ITenantScopedRequest
{
    public string? TenantId { get; set; }
}

public sealed record CreateInstanceResult(
    string InstanceId,
    string InstanceName,
    string ApiKey,
    string Status);

// ── Validator ─────────────────────────────────────────────────────────────────

public sealed class CreateInstanceCommandValidator : AbstractValidator<CreateInstanceCommand>
{
    public CreateInstanceCommandValidator()
    {
        RuleFor(x => x.InstanceName)
            .NotEmpty().WithMessage("instanceName is required")
            .MaximumLength(255).WithMessage("instanceName must not exceed 255 characters")
            .Matches("^[a-zA-Z0-9_-]+$").WithMessage("instanceName may only contain letters, numbers, hyphens, and underscores");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("token is required")
            .MinimumLength(8).WithMessage("token must be at least 8 characters");

        RuleFor(x => x.Number)
            .Matches(@"^\+?[1-9]\d{6,14}$").When(x => x.Number is not null)
            .WithMessage("number must be a valid international phone number");

        RuleFor(x => x.WebhookUrl)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _)).When(x => x.WebhookUrl is not null)
            .WithMessage("webhookUrl must be a valid absolute URL");
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class CreateInstanceCommandHandler(
    IUnitOfWork uow,
    IWhatsAppEngine engine,
    ICacheService cache) : IRequestHandler<CreateInstanceCommand, CreateInstanceResult>
{
    public async Task<CreateInstanceResult> Handle(CreateInstanceCommand cmd, CancellationToken ct)
    {
        // Check uniqueness
        if (await uow.Instances.ExistsAsync(cmd.InstanceName, ct))
            throw new InvalidOperationException($"Instance '{cmd.InstanceName}' already exists.");

        // Hash the API key
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(cmd.Token)));

        // Create domain entity (pure POCO)
        var instance = new Instance
        {
            TenantId = cmd.TenantId,
            Name = cmd.InstanceName,
            ApiKeyHash = hash,
            Number = cmd.Number,
            Integration = "WHATSAPP-BAILEYS",
            ConnectionStatus = "close"
        };

        // Persist via UoW
        await uow.Instances.AddAsync(instance, ct);

        // Create webhook config if URL provided
        if (!string.IsNullOrWhiteSpace(cmd.WebhookUrl))
        {
            var wh = new WebhookConfig
            {
                InstanceId = instance.Id,
                Url = cmd.WebhookUrl,
                EventsJson = cmd.WebhookEvents is { Length: > 0 }
                    ? System.Text.Json.JsonSerializer.Serialize(cmd.WebhookEvents)
                    : "[]",
                Enabled = true
            };
            await uow.Webhooks.AddAsync(wh, ct);
        }

        await uow.SaveChangesAsync(ct);

        // Start the engine session
        await engine.StartAsync(instance.Id, instance.Name, instance.Number, ct);

        await cache.RemoveByTagAsync(EvolutionApi.Application.Features.Instances.Caching.InstanceCacheKeys.Tag, ct);

        return new CreateInstanceResult(instance.Id, instance.Name, cmd.Token, "created");
    }
}
