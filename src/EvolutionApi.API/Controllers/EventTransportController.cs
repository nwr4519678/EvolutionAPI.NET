using AutoMapper;
using EvolutionApi.Application.Features.Integrations.EventTransport.Commands.SetNats;
using EvolutionApi.Application.Features.Integrations.EventTransport.Commands.SetRabbitMq;
using EvolutionApi.Application.Features.Integrations.EventTransport.Commands.SetSqs;
using EvolutionApi.Application.Features.Integrations.EventTransport.DTOs;
using EvolutionApi.Application.Features.Integrations.EventTransport.Queries.FindNats;
using EvolutionApi.Application.Features.Integrations.EventTransport.Queries.FindRabbitMq;
using EvolutionApi.Application.Features.Integrations.EventTransport.Queries.FindSqs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EvolutionApi.API.Controllers;

// ── RabbitMQ ─────────────────────────────────────────────────────────────────

[ApiController]
[Route("rabbitmq")]
[EnableRateLimiting("integration")]
public sealed class RabbitMqController(ISender mediator, IMapper mapper) : ControllerBase
{
    [HttpPost("set/{instanceName}")]
    public async Task<IActionResult> Set(string instanceName, [FromBody] SetRabbitMqDto body, CancellationToken ct)
        => Ok(await mediator.Send(mapper.Map<SetRabbitMqCommand>(body, opts => opts.Items["instanceName"] = instanceName), ct));

    [HttpGet("find/{instanceName}")]
    public async Task<IActionResult> Find(string instanceName, CancellationToken ct)
        => Ok(await mediator.Send(new FindRabbitMqQuery(instanceName), ct));
}

// ── SQS ──────────────────────────────────────────────────────────────────────

[ApiController]
[Route("sqs")]
[EnableRateLimiting("integration")]
public sealed class SqsController(ISender mediator, IMapper mapper) : ControllerBase
{
    [HttpPost("set/{instanceName}")]
    public async Task<IActionResult> Set(string instanceName, [FromBody] SetSqsDto body, CancellationToken ct)
        => Ok(await mediator.Send(mapper.Map<SetSqsCommand>(body, opts => opts.Items["instanceName"] = instanceName), ct));

    [HttpGet("find/{instanceName}")]
    public async Task<IActionResult> Find(string instanceName, CancellationToken ct)
        => Ok(await mediator.Send(new FindSqsQuery(instanceName), ct));
}

// ── NATS ─────────────────────────────────────────────────────────────────────

[ApiController]
[Route("nats")]
[EnableRateLimiting("integration")]
public sealed class NatsController(ISender mediator, IMapper mapper) : ControllerBase
{
    [HttpPost("set/{instanceName}")]
    public async Task<IActionResult> Set(string instanceName, [FromBody] SetNatsDto body, CancellationToken ct)
        => Ok(await mediator.Send(mapper.Map<SetNatsCommand>(body, opts => opts.Items["instanceName"] = instanceName), ct));

    [HttpGet("find/{instanceName}")]
    public async Task<IActionResult> Find(string instanceName, CancellationToken ct)
        => Ok(await mediator.Send(new FindNatsQuery(instanceName), ct));
}
