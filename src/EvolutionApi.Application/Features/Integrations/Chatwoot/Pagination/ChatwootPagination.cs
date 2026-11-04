using EvolutionApi.Application.Common.Pagination;

namespace EvolutionApi.Application.Features.Integrations.Chatwoot.Pagination;

/// <summary>
/// Pagination configuration for Chatwoot.
/// </summary>
public sealed record ChatwootPaginationParams(
    int PageNumber = 1,
    int PageSize = 20) : PaginationParams(PageNumber, PageSize);
