using EvolutionApi.Application.Common.Pagination;

namespace EvolutionApi.Application.Features.Integrations.OpenAi.Pagination;

/// <summary>
/// Pagination configuration for OpenAI sessions.
/// </summary>
public sealed record OpenAiPaginationParams(
    int PageNumber = 1,
    int PageSize = 20) : PaginationParams(PageNumber, PageSize);
