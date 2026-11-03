using AutoMapper;
using EvolutionApi.Application.Features.Integrations.Chatwoot.Commands.SetChatwoot;
using EvolutionApi.Application.Features.Integrations.Chatwoot.DTOs;
using EvolutionApi.Application.Features.Integrations.Chatwoot.Queries.FindChatwoot;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EvolutionApi.API.Controllers;

/// <summary>
/// Chatwoot integration controller.
/// Thin controller — binds incoming DTOs, maps via AutoMapper, merges route data, and dispatches via MediatR.
/// </summary>
[ApiController]
[Route("chatwoot")]
[EnableRateLimiting("integration")]
public sealed class ChatwootController(ISender mediator, IMapper mapper) : ControllerBase
{
    [HttpPost("set/{instanceName}")]
    public async Task<IActionResult> Set(
        string instanceName, [FromBody] SetChatwootDto body, CancellationToken ct)
        => Ok(await mediator.Send(
            mapper.Map<SetChatwootCommand>(body, opts => opts.Items["instanceName"] = instanceName), ct));

    [HttpGet("find/{instanceName}")]
    public async Task<IActionResult> Find(
        string instanceName, CancellationToken ct)
        => Ok(await mediator.Send(new FindChatwootQuery(instanceName), ct));
}
