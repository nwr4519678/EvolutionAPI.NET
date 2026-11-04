using EvolutionApi.Application.Common.Pagination;

namespace EvolutionApi.Application.Features.Calls.Pagination;

/// <summary>
/// Pagination configuration for Calls.
/// </summary>
public sealed record CallPaginationParams(
    int PageNumber = 1,
    int PageSize = 20) : PaginationParams(PageNumber, PageSize);
