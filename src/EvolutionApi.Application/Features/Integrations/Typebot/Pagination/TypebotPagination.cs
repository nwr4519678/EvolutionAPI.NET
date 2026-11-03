using EvolutionApi.Application.Common.Pagination;

namespace EvolutionApi.Application.Features.Integrations.Typebot.Pagination;

/// <summary>
/// Pagination configuration for Typebot sessions.
/// </summary>
public sealed record TypebotPaginationParams(
    int PageNumber = 1,
    int PageSize = 20) : PaginationParams(PageNumber, PageSize);
