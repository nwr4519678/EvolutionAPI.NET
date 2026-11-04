using EvolutionApi.Application.Features.Instances.Commands.ConnectInstance;
using EvolutionApi.Application.Features.Instances.Commands.CreateInstance;
using EvolutionApi.Application.Features.Instances.Commands.DeleteInstance;
using EvolutionApi.Application.Features.Instances.Commands.DisconnectInstance;
using EvolutionApi.Application.Features.Instances.Queries.GetInstances;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EvolutionApi.API.Controllers;

/// <summary>
/// Instance management controller.
/// Contains zero business logic - all operations are dispatched to MediatR.
/// </summary>
[ApiController]
[Route("instance")]
[EnableRateLimiting("instance")]
public sealed class InstanceController(ISender mediator) : ControllerBase
{
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateInstanceCommand command,
     CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return Created($"/instance/fetchInstances?instanceName={result.InstanceName}", result);
    }

    [HttpGet("connect/{instanceName}")]
    public async Task<IActionResult> Connect(
        string instanceName, [FromQuery] string? number, CancellationToken ct)
        => Ok(await mediator.Send(new ConnectInstanceCommand(instanceName, number), ct));

    [HttpDelete("logout/{instanceName}")]
    public async Task<IActionResult> Disconnect(
        string instanceName, CancellationToken ct)
        => Ok(new { status = "SUCCESS", disconnected = await mediator.Send(
            new DisconnectInstanceCommand(instanceName), ct) });

    [HttpDelete("delete/{instanceName}")]
    public async Task<IActionResult> Delete(
        string instanceName, CancellationToken ct)
        => Ok(new { status = "SUCCESS", deleted = await mediator.Send(
            new DeleteInstanceCommand(instanceName), ct) });

    [HttpGet("fetchInstances")]
    public async Task<IActionResult> GetInstances(
        [FromQuery] string? instanceName, CancellationToken ct)
        => Ok(await mediator.Send(new GetInstancesQuery(instanceName), ct));
}
