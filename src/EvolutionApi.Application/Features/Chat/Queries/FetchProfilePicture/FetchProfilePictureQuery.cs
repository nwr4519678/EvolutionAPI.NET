using EvolutionApi.Application.Common.Abstractions;
using EvolutionApi.Application.Common.Caching;
using EvolutionApi.Application.Features.Chat.Caching;
using EvolutionApi.Domain.Interfaces;
using FluentValidation;
using System.Text.Json;

namespace EvolutionApi.Application.Features.Chat.Queries.FetchProfilePicture;

// ── Query ─────────────────────────────────────────────────────────────────────

public sealed record FetchProfilePictureQuery(
    string  InstanceName,
    string? Number = null,
    string? Jid    = null) : IQuery<JsonElement>, ICacheableQuery
{
    public string    CacheKey   => ChatCacheKeys.ProfilePicture(InstanceName, Number ?? Jid ?? string.Empty);
    public TimeSpan? Expiration => ChatCacheKeys.Expiration;
    public string?   CacheTag   => ChatCacheKeys.Tag(InstanceName);
}

// ── Validator ─────────────────────────────────────────────────────────────────

public sealed class FetchProfilePictureQueryValidator : AbstractValidator<FetchProfilePictureQuery>
{
    public FetchProfilePictureQueryValidator()
    {
        RuleFor(x => x.InstanceName).NotEmpty().WithMessage("instanceName is required");
        RuleFor(x => x)
            .Must(x => !string.IsNullOrEmpty(x.Number) || !string.IsNullOrEmpty(x.Jid))
            .WithMessage("number or jid is required");
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class FetchProfilePictureQueryHandler(
    IInstanceRepository instances,
    IWhatsAppEngine engine) : MediatR.IRequestHandler<FetchProfilePictureQuery, JsonElement>
{
    public async Task<JsonElement> Handle(FetchProfilePictureQuery query, CancellationToken ct)
    {
        var instance = await instances.GetByNameAsync(query.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{query.InstanceName}' not found.");

        var target = query.Number ?? query.Jid ?? string.Empty;
        return await engine.FetchProfilePictureUrlAsync(instance.Id, target, ct);
    }
}
