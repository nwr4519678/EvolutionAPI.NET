using AutoMapper;
using EvolutionApi.Application.Features.Proxy.Commands.SetProxy;
using EvolutionApi.Application.Features.Proxy.DTOs;
using EvolutionApi.Application.Features.Proxy.Queries.FindProxy;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EvolutionApi.API.Controllers;

/// <summary>
/// Proxy configuration controller.
/// Thin controller — binds DTO, maps via AutoMapper (injecting instanceName), dispatches via MediatR.
/// </summary>
[ApiController]
[Route("proxy")]
[EnableRateLimiting("settings")]
public sealed class ProxyController(ISender mediator, IMapper mapper) : ControllerBase
{
    [HttpPost("set/{instanceName}")]
    public async Task<IActionResult> Set(
        string instanceName, [FromBody] SetProxyDto body, CancellationToken ct)
        => Ok(await mediator.Send(
            mapper.Map<SetProxyCommand>(body, opts => opts.Items["instanceName"] = instanceName), ct));

    [HttpGet("find/{instanceName}")]
    public async Task<IActionResult> Find(string instanceName, CancellationToken ct)
        => Ok(await mediator.Send(new FindProxyQuery(instanceName), ct));
}
