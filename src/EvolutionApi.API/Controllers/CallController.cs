using AutoMapper;
using EvolutionApi.Application.Features.Calls.Commands.OfferCall;
using EvolutionApi.Application.Features.Calls.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EvolutionApi.API.Controllers;

/// <summary>
/// Call controller.
/// Thin controller — binds incoming DTO, maps to MediatR Command via AutoMapper, and merges route data.
/// </summary>
[ApiController]
[Route("call")]
[EnableRateLimiting("call")]
public sealed class CallController(ISender mediator, IMapper mapper) : ControllerBase
{
    [HttpPost("offer/{instanceName}")]
    public async Task<IActionResult> Offer(
        string instanceName, [FromBody] OfferCallDto body, CancellationToken ct)
        => Ok(await mediator.Send(
            mapper.Map<OfferCallCommand>(body, opts => opts.Items["instanceName"] = instanceName), ct));
}
