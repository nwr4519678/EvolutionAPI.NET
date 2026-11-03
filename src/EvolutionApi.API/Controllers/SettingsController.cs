using AutoMapper;
using EvolutionApi.Application.Features.Settings.Commands.SetSettings;
using EvolutionApi.Application.Features.Settings.DTOs;
using EvolutionApi.Application.Features.Settings.Queries.GetSettings;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EvolutionApi.API.Controllers;

/// <summary>
/// Instance settings controller.
/// Thin controller — binds incoming DTO, maps to MediatR Command via AutoMapper
/// , and merges route data.
/// </summary>
[ApiController]
[Route("settings")]
[EnableRateLimiting("settings")]
public sealed class SettingsController(ISender mediator, IMapper mapper) : ControllerBase
{
    [HttpPost("set/{instanceName}")]
    public async Task<IActionResult> Set(
        string instanceName, [FromBody] SetSettingsDto body, CancellationToken ct)
        => Ok(await mediator.Send(
            mapper.Map<SetSettingsCommand>(body, opts => opts.Items["instanceName"] = instanceName), ct));

    [HttpGet("find/{instanceName}")]
    public async Task<IActionResult> Find(
        string instanceName, CancellationToken ct)
        => Ok(await mediator.Send(new GetSettingsQuery(instanceName), ct));
}
