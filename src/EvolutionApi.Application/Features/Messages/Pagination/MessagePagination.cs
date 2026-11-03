using EvolutionApi.Application.Common.Pagination;

namespace EvolutionApi.Application.Features.Messages.Pagination;

/// <summary>
/// Pagination configuration for Messages.
/// </summary>
public sealed record MessagePaginationParams(
    string? RemoteJid = null,
    int PageNumber = 1,
    int PageSize = 20) : PaginationParams(PageNumber, PageSize);
