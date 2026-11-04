using EvolutionApi.Application.Common.Abstractions;
using EvolutionApi.Application.Common.Caching;
using EvolutionApi.Application.Features.Groups.Caching;
using EvolutionApi.Domain.Interfaces;
using FluentValidation;
using MediatR;
using System.Text.Json;

namespace EvolutionApi.Application.Features.Groups.Queries.GetGroupInfo;

// ── Query ─────────────────────────────────────────────────────────────────────

public sealed record GetGroupInfoQuery(
    string InstanceName,
    string GroupJid) : IRequest<JsonElement>, ICacheableQuery
{
    public string CacheKey => GroupCacheKeys.Info(InstanceName, GroupJid);
    public TimeSpan? Expiration => GroupCacheKeys.Expiration;
    public string? CacheTag => GroupCacheKeys.Tag(InstanceName);
}

// ── Validator ─────────────────────────────────────────────────────────────────

public sealed class GetGroupInfoQueryValidator : AbstractValidator<GetGroupInfoQuery>
{
    public GetGroupInfoQueryValidator()
    {
        RuleFor(x => x.InstanceName).NotEmpty();
        RuleFor(x => x.GroupJid).NotEmpty().WithMessage("groupJid is required");
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class GetGroupInfoQueryHandler(
    IInstanceRepository instances,
    IWhatsAppEngine engine) : IRequestHandler<GetGroupInfoQuery, JsonElement>
{
    public async Task<JsonElement> Handle(GetGroupInfoQuery query, CancellationToken ct)
    {
        var instance = await instances.GetByNameAsync(query.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{query.InstanceName}' not found.");

        var groupJid = query.GroupJid.Contains('@') ? query.GroupJid : $"{query.GroupJid}@g.us";
        return await engine.FetchGroupMetadataAsync(instance.Id, groupJid, ct);
    }
}
