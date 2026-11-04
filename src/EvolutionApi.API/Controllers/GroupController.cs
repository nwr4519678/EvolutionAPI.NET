using AutoMapper;
using EvolutionApi.Application.Features.Groups.Commands.CreateGroup;
using EvolutionApi.Application.Features.Groups.Commands.LeaveGroup;
using EvolutionApi.Application.Features.Groups.Commands.UpdateGroupParticipants;
using EvolutionApi.Application.Features.Groups.Commands.UpdateGroupSubject;
using EvolutionApi.Application.Features.Groups.DTOs;
using EvolutionApi.Application.Features.Groups.Queries.GetAllGroups;
using EvolutionApi.Application.Features.Groups.Queries.GetGroupInfo;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EvolutionApi.API.Controllers;

/// <summary>
/// Group operations controller.
/// Thin controller — maps DTOs, merges route params, and dispatches via MediatR.
/// </summary>
[ApiController]
[Route("group")]
[EnableRateLimiting("group")]
public sealed class GroupController(ISender mediator, IMapper mapper) : ControllerBase
{
    [HttpPost("create/{instanceName}")]
    public async Task<IActionResult> Create(
        string instanceName, [FromBody] CreateGroupDto body, CancellationToken ct)
        => StatusCode(201, await mediator.Send(
            mapper.Map<CreateGroupCommand>(body, opts => opts.Items["instanceName"] = instanceName), ct));

    [HttpGet("findGroupInfos/{instanceName}")]
    public async Task<IActionResult> FindGroupInfos(
        string instanceName, [FromQuery] string groupJid, CancellationToken ct)
        => Ok(await mediator.Send(
            new GetGroupInfoQuery(instanceName, groupJid), ct));

    [HttpGet("fetchAllGroups/{instanceName}")]
    public async Task<IActionResult> FetchAllGroups(
        string instanceName, CancellationToken ct)
        => Ok(await mediator.Send(
            new GetAllGroupsQuery(instanceName), ct));

    [HttpGet("participants/{instanceName}")]
    public async Task<IActionResult> Participants(
        string instanceName, [FromQuery] string groupJid, CancellationToken ct)
        => Ok(await mediator.Send(
            new GetGroupInfoQuery(instanceName, groupJid), ct));

    [HttpPost("updateParticipant/{instanceName}")]
    public async Task<IActionResult> UpdateParticipants(
        string instanceName, [FromBody] UpdateGroupParticipantsDto body, CancellationToken ct)
        => Ok(await mediator.Send(
            mapper.Map<UpdateGroupParticipantsCommand>(body, opts => opts.Items["instanceName"] = instanceName), ct));

    [HttpPost("updateGroupSubject/{instanceName}")]
    public async Task<IActionResult> UpdateGroupSubject(
        string instanceName, [FromBody] UpdateGroupSubjectDto body, CancellationToken ct)
        => Ok(await mediator.Send(
            mapper.Map<UpdateGroupSubjectCommand>(body, opts => opts.Items["instanceName"] = instanceName), ct));

    [HttpDelete("leaveGroup/{instanceName}")]
    public async Task<IActionResult> LeaveGroup(
        string instanceName, [FromQuery] string groupJid, CancellationToken ct)
        => Ok(await mediator.Send(
            new LeaveGroupCommand(instanceName, groupJid), ct));
}
