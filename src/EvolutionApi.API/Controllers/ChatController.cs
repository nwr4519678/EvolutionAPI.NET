using AutoMapper;
using EvolutionApi.Application.Features.Chat.Commands.ArchiveChat;
using EvolutionApi.Application.Features.Chat.Commands.DeleteMessage;
using EvolutionApi.Application.Features.Chat.Commands.MarkMessageRead;
using EvolutionApi.Application.Features.Chat.DTOs;
using EvolutionApi.Application.Features.Chat.Queries.CheckWhatsAppNumbers;
using EvolutionApi.Application.Features.Chat.Queries.FetchProfilePicture;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EvolutionApi.API.Controllers;

/// <summary>
/// Chat operations controller.
/// Thin controller — binds incoming DTOs, maps via AutoMapper, and dispatches via MediatR.
/// </summary>
[ApiController]
[Route("chat")]
[EnableRateLimiting("chat")]
public sealed class ChatController(ISender mediator, IMapper mapper) : ControllerBase
{
    [HttpPost("whatsappNumbers/{instanceName}")]
    public async Task<IActionResult> WhatsAppNumbers(
        string instanceName, [FromBody] CheckWhatsAppNumbersDto body, CancellationToken ct)
        => Ok(await mediator.Send(
            mapper.Map<CheckWhatsAppNumbersQuery>(body, opts => opts.Items["instanceName"] = instanceName), ct));

    [HttpPost("markMessageAsRead/{instanceName}")]
    public async Task<IActionResult> MarkMessageAsRead(
        string instanceName, [FromBody] MarkMessageReadDto body, CancellationToken ct)
        => Ok(await mediator.Send(
            mapper.Map<MarkMessageReadCommand>(body, opts => opts.Items["instanceName"] = instanceName), ct));

    [HttpPost("archiveChat/{instanceName}")]
    public async Task<IActionResult> ArchiveChat(
        string instanceName, [FromBody] ArchiveChatDto body, CancellationToken ct)
        => Ok(await mediator.Send(
            mapper.Map<ArchiveChatCommand>(body, opts => opts.Items["instanceName"] = instanceName), ct));

    [HttpDelete("deleteMessageForEveryone/{instanceName}")]
    public async Task<IActionResult> DeleteMessageForEveryone(
        string instanceName, [FromBody] DeleteMessageDto body, CancellationToken ct)
        => Ok(await mediator.Send(
            mapper.Map<DeleteMessageCommand>(body, opts => opts.Items["instanceName"] = instanceName), ct));

    [HttpPost("fetchProfilePictureUrl/{instanceName}")]
    public async Task<IActionResult> FetchProfilePictureUrl(
        string instanceName, [FromBody] FetchProfilePictureDto body, CancellationToken ct)
        => Ok(await mediator.Send(
            mapper.Map<FetchProfilePictureQuery>(body, opts => opts.Items["instanceName"] = instanceName), ct));
}
