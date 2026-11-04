using EvolutionApi.Application.Common.Pagination;

namespace EvolutionApi.Application.Features.Webhooks.Pagination;

/// <summary>
/// Pagination configuration for Webhooks.
/// </summary>
public sealed record WebhookPaginationParams(
    int PageNumber = 1,
    int PageSize = 20) : PaginationParams(PageNumber, PageSize);
