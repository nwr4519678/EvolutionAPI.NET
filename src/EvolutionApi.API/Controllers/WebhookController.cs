using AutoMapper;
using EvolutionApi.Application.Features.Webhooks.Commands.SetWebhook;
using EvolutionApi.Application.Features.Webhooks.DTOs;
using EvolutionApi.Application.Features.Webhooks.Queries.FindWebhook;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EvolutionApi.API.Controllers;

/// <summary>
/// Webhook management controller.
/// Thin controller — binds incoming DTO, maps via AutoMapper (injecting instanceName), dispatches via MediatR.
/// </summary>
[ApiController]
[Route("webhook")]
[EnableRateLimiting("settings")]
public sealed class WebhookController(ISender mediator, IMapper mapper) : ControllerBase
{
    [HttpPost("set/{instanceName}")]
    public async Task<IActionResult> SetWebhook(
        string instanceName, [FromBody] SetWebhookDto body, CancellationToken ct)
        => Ok(await mediator.Send(
            mapper.Map<SetWebhookCommand>(body, opts => opts.Items["instanceName"] = instanceName), ct));

    [HttpGet("find/{instanceName}")]
    public async Task<IActionResult> FindWebhook(string instanceName, CancellationToken ct)
        => Ok(await mediator.Send(new FindWebhookQuery(instanceName), ct));
}
