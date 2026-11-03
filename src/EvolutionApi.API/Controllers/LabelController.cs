using AutoMapper;
using EvolutionApi.Application.Features.Labels.Commands.CreateLabel;
using EvolutionApi.Application.Features.Labels.Commands.DeleteLabel;
using EvolutionApi.Application.Features.Labels.Commands.HandleLabel;
using EvolutionApi.Application.Features.Labels.Commands.UpdateLabel;
using EvolutionApi.Application.Features.Labels.DTOs;
using EvolutionApi.Application.Features.Labels.Queries.FindLabels;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EvolutionApi.API.Controllers;

/// <summary>
/// Label management controller (WhatsApp Business feature).
/// Thin controller — binds DTOs, maps via AutoMapper (injecting instanceName into ctx.Items), dispatches via MediatR.
/// </summary>
[ApiController]
[Route("label")]
[EnableRateLimiting("label")]
public sealed class LabelController(ISender mediator, IMapper mapper) : ControllerBase
{
    [HttpGet("findLabels/{instanceName}")]
    public async Task<IActionResult> FindLabels(
        string instanceName, CancellationToken ct)
        => Ok(await mediator.Send(new FindLabelsQuery(instanceName), ct));

    [HttpPost("create/{instanceName}")]
    public async Task<IActionResult> CreateLabel(
        string instanceName, [FromBody] CreateLabelDto body, CancellationToken ct)
        => StatusCode(201, await mediator.Send(
            mapper.Map<CreateLabelCommand>(body, opts => opts.Items["instanceName"] = instanceName), ct));

    [HttpPut("update/{instanceName}")]
    public async Task<IActionResult> UpdateLabel(
        string instanceName, [FromBody] UpdateLabelDto body, CancellationToken ct)
        => Ok(await mediator.Send(
            mapper.Map<UpdateLabelCommand>(body, opts => opts.Items["instanceName"] = instanceName), ct));

    [HttpDelete("delete/{instanceName}")]
    public async Task<IActionResult> DeleteLabel(
        string instanceName, [FromBody] DeleteLabelDto body, CancellationToken ct)
        => Ok(await mediator.Send(
            mapper.Map<DeleteLabelCommand>(body, opts => opts.Items["instanceName"] = instanceName), ct));

    [HttpPost("handleLabel/{instanceName}")]
    public async Task<IActionResult> HandleLabel(
        string instanceName, [FromBody] HandleLabelDto body, CancellationToken ct)
        => Ok(await mediator.Send(
            mapper.Map<HandleLabelCommand>(body, opts => opts.Items["instanceName"] = instanceName), ct));
}
