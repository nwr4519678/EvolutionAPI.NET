using EvolutionApi.Application.Common.Pagination;

namespace EvolutionApi.Application.Features.Integrations.EventTransport.Pagination;

/// <summary>
/// Pagination configuration for EventTransport.
/// </summary>
public sealed record EventTransportPaginationParams(
    int PageNumber = 1,
    int PageSize = 20) : PaginationParams(PageNumber, PageSize);
