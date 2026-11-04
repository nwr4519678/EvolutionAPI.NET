using EvolutionApi.Application.Common.Pagination;

namespace EvolutionApi.Application.Features.Proxy.Pagination;

/// <summary>
/// Pagination configuration for Proxy.
/// </summary>
public sealed record ProxyPaginationParams(
    int PageNumber = 1,
    int PageSize = 20) : PaginationParams(PageNumber, PageSize);
