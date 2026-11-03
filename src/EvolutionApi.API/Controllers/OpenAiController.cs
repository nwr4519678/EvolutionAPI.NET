using AutoMapper;
using EvolutionApi.Application.Features.Integrations.OpenAi.Commands.SetOpenAi;
using EvolutionApi.Application.Features.Integrations.OpenAi.Commands.StartOpenAi;
using EvolutionApi.Application.Features.Integrations.OpenAi.DTOs;
using EvolutionApi.Application.Features.Integrations.OpenAi.Queries.FindOpenAi;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EvolutionApi.API.Controllers;

/// <summary>
/// OpenAI integration controller.
/// Thin controller — binds incoming DTOs, maps to MediatR Commands via AutoMapper,
///  and merges route data.
/// </summary>
[ApiController]
[Route("openai")]
[EnableRateLimiting("integration")]
public sealed class OpenAiController(ISender mediator, IMapper mapper) : ControllerBase
{
    [HttpPost("set/{instanceName}")]
    public async Task<IActionResult> Set(
        string instanceName, [FromBody] SetOpenAiDto body, CancellationToken ct)
        => Ok(await mediator.Send(
            mapper.Map<SetOpenAiCommand>(body, opts => opts.Items["instanceName"] = instanceName), ct));

    [HttpGet("find/{instanceName}")]
    public async Task<IActionResult> Find(
        string instanceName, CancellationToken ct)
        => Ok(await mediator.Send(new FindOpenAiQuery(instanceName), ct));

    [HttpPost("start/{instanceName}")]
    public async Task<IActionResult> Start(
        string instanceName, [FromBody] StartOpenAiDto body, CancellationToken ct)
        => Ok(await mediator.Send(
            mapper.Map<StartOpenAiCommand>(body, opts => opts.Items["instanceName"] = instanceName), ct));
}
