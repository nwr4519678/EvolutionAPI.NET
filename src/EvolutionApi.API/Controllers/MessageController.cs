using System.Text.Json;
using System.Text.Json.Nodes;
using EvolutionApi.Application.Features.Messages.Commands.SendMessage;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EvolutionApi.API.Controllers;

/// <summary>
/// Message dispatch controller.
/// Contains zero business logic — all operations are dispatched to MediatR.
/// Accepts <see cref="JsonNode"/> body parameters so that the OpenAPI schema exporter
/// can produce a valid open-object schema (<c>{}</c>) for Scalar, instead of crashing
/// on <see cref="JsonElement"/> which is a raw JSON container the exporter cannot introspect.
/// </summary>
[ApiController]
[Route("message")]
[EnableRateLimiting("message")]
public sealed class MessageController(ISender mediator) : ControllerBase
{
    // Converts the nullable JsonNode received from the model binder into a JsonElement
    // that the internal engine and command layer expects.
    private static JsonElement ToElement(JsonNode? node) =>
        node is null ? default : JsonSerializer.SerializeToElement(node);

    [HttpPost("sendText/{instanceName}")]
    public async Task<IActionResult> SendText(
        string instanceName, [FromBody] JsonNode? payload, CancellationToken ct)
        => Ok(await mediator.Send(new SendMessageCommand(instanceName, "sendText", ToElement(payload)), ct));

    [HttpPost("sendStatus/{instanceName}")]
    public async Task<IActionResult> SendStatus(
        string instanceName, [FromBody] JsonNode? payload, CancellationToken ct)
        => Ok(await mediator.Send(new SendMessageCommand(instanceName, "sendStatus", ToElement(payload)), ct));

    [HttpPost("sendMedia/{instanceName}")]
    public async Task<IActionResult> SendMedia(
        string instanceName, [FromBody] JsonNode? payload, CancellationToken ct)
        => Ok(await mediator.Send(new SendMessageCommand(instanceName, "sendMedia", ToElement(payload)), ct));

    [HttpPost("sendWhatsAppAudio/{instanceName}")]
    public async Task<IActionResult> SendWhatsAppAudio(
        string instanceName, [FromBody] JsonNode? payload, CancellationToken ct)
        => Ok(await mediator.Send(new SendMessageCommand(instanceName, "sendWhatsAppAudio", ToElement(payload)), ct));

    [HttpPost("sendPtv/{instanceName}")]
    public async Task<IActionResult> SendPtv(
        string instanceName, [FromBody] JsonNode? payload, CancellationToken ct)
        => Ok(await mediator.Send(new SendMessageCommand(instanceName, "sendPtv", ToElement(payload)), ct));

    [HttpPost("sendSticker/{instanceName}")]
    public async Task<IActionResult> SendSticker(
        string instanceName, [FromBody] JsonNode? payload, CancellationToken ct)
        => Ok(await mediator.Send(new SendMessageCommand(instanceName, "sendSticker", ToElement(payload)), ct));

    [HttpPost("sendLocation/{instanceName}")]
    public async Task<IActionResult> SendLocation(
        string instanceName, [FromBody] JsonNode? payload, CancellationToken ct)
        => Ok(await mediator.Send(new SendMessageCommand(instanceName, "sendLocation", ToElement(payload)), ct));

    [HttpPost("sendContact/{instanceName}")]
    public async Task<IActionResult> SendContact(
        string instanceName, [FromBody] JsonNode? payload, CancellationToken ct)
        => Ok(await mediator.Send(new SendMessageCommand(instanceName, "sendContact", ToElement(payload)), ct));

    [HttpPost("sendReaction/{instanceName}")]
    public async Task<IActionResult> SendReaction(
        string instanceName, [FromBody] JsonNode? payload, CancellationToken ct)
        => Ok(await mediator.Send(new SendMessageCommand(instanceName, "sendReaction", ToElement(payload)), ct));

    [HttpPost("sendPoll/{instanceName}")]
    public async Task<IActionResult> SendPoll(
        string instanceName, [FromBody] JsonNode? payload, CancellationToken ct)
        => Ok(await mediator.Send(new SendMessageCommand(instanceName, "sendPoll", ToElement(payload)), ct));

    [HttpPost("sendList/{instanceName}")]
    public async Task<IActionResult> SendList(
        string instanceName, [FromBody] JsonNode? payload, CancellationToken ct)
        => Ok(await mediator.Send(new SendMessageCommand(instanceName, "sendList", ToElement(payload)), ct));

    [HttpPost("sendButtons/{instanceName}")]
    public async Task<IActionResult> SendButtons(
        string instanceName, [FromBody] JsonNode? payload, CancellationToken ct)
        => Ok(await mediator.Send(new SendMessageCommand(instanceName, "sendButtons", ToElement(payload)), ct));

    [HttpPost("sendTemplate/{instanceName}")]
    public async Task<IActionResult> SendTemplate(
        string instanceName, [FromBody] JsonNode? payload, CancellationToken ct)
        => Ok(await mediator.Send(new SendMessageCommand(instanceName, "sendTemplate", ToElement(payload)), ct));
}
