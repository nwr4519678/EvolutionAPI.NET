using EvolutionApi.Application.Common.Abstractions;
using EvolutionApi.Domain.Interfaces;
using FluentValidation;
using System.Text.Json;

namespace EvolutionApi.Application.Features.Chat.Queries.CheckWhatsAppNumbers;

// ── Query ─────────────────────────────────────────────────────────────────────

public sealed record CheckWhatsAppNumbersQuery(
    string    InstanceName,
    string[]? Numbers = null) : IQuery<JsonElement>;

// ── Validator ─────────────────────────────────────────────────────────────────

public sealed class CheckWhatsAppNumbersQueryValidator : AbstractValidator<CheckWhatsAppNumbersQuery>
{
    public CheckWhatsAppNumbersQueryValidator()
    {
        RuleFor(x => x.InstanceName).NotEmpty().WithMessage("instanceName is required");
        RuleFor(x => x.Numbers).NotEmpty().WithMessage("numbers must contain at least one item");
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class CheckWhatsAppNumbersQueryHandler(
    IInstanceRepository instances,
    IWhatsAppEngine engine) : MediatR.IRequestHandler<CheckWhatsAppNumbersQuery, JsonElement>
{
    public async Task<JsonElement> Handle(CheckWhatsAppNumbersQuery query, CancellationToken ct)
    {
        var instance = await instances.GetByNameAsync(query.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{query.InstanceName}' not found.");

        return await engine.CheckWhatsAppNumbersAsync(instance.Id, query.Numbers ?? [], ct);
    }
}
