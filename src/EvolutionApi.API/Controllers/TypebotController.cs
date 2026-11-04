using AutoMapper;
using EvolutionApi.Application.Features.Integrations.Typebot.Commands.SetTypebot;
using EvolutionApi.Application.Features.Integrations.Typebot.Commands.TypebotChangeStatus;
using EvolutionApi.Application.Features.Integrations.Typebot.Commands.TypebotStart;
using EvolutionApi.Application.Features.Integrations.Typebot.DTOs;
using EvolutionApi.Application.Features.Integrations.Typebot.Queries.GetTypebot;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EvolutionApi.API.Controllers;

/// <summary>
/// Typebot chatbot integration controller.
/// Thin controller — binds incoming DTO, maps to MediatR Command via 
/// , and merges route data.
/// </summary>
[ApiController]
[Route("typebot")]
[EnableRateLimiting("integration")]
public sealed class TypebotController(ISender mediator, IMapper mapper) : ControllerBase
{
    [HttpPost("setting/{instanceName}")]
    public async Task<IActionResult> SetSetting(
        string instanceName, [FromBody] SetTypebotDto body, CancellationToken ct)
        => Ok(await mediator.Send(
            mapper.Map<SetTypebotCommand>(body, opts => opts.Items["instanceName"] = instanceName), ct));

    [HttpGet("setting/{instanceName}")]
    public async Task<IActionResult> GetSetting(
        string instanceName, CancellationToken ct)
        => Ok(await mediator.Send(new GetTypebotQuery(instanceName), ct));

    [HttpPost("start/{instanceName}")]
    public async Task<IActionResult> StartSession(
        string instanceName, [FromBody] TypebotStartDto body, CancellationToken ct)
        => Ok(await mediator.Send(
            mapper.Map<TypebotStartCommand>(body, opts => opts.Items["instanceName"] = instanceName), ct));

    [HttpPost("changeStatus/{instanceName}")]
    public async Task<IActionResult> ChangeStatus(
        string instanceName, [FromBody] TypebotStatusDto body, CancellationToken ct)
        => Ok(await mediator.Send(
            mapper.Map<TypebotChangeStatusCommand>(body, opts => opts.Items["instanceName"] = instanceName), ct));
}
