namespace EvolutionApi.Application.Features.Groups.DTOs;

public sealed record CreateGroupDto(
    string Subject = "",
    string[]? Participants = null);

public sealed record UpdateGroupParticipantsDto(
    string GroupJid = "",
    string[]? Participants = null,
    string Action = "");

public sealed record UpdateGroupSubjectDto(
    string GroupJid = "",
    string Subject = "");
