using System.Text.Json.Nodes;

namespace EvolutionApi.Application.Features.Calls.DTOs;

public sealed record OfferCallDto(
    JsonNode? Payload = null);
