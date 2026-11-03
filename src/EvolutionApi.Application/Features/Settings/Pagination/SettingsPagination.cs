using EvolutionApi.Application.Common.Pagination;

namespace EvolutionApi.Application.Features.Settings.Pagination;

/// <summary>
/// Pagination configuration for Settings.
/// </summary>
public sealed record SettingsPaginationParams(
    int PageNumber = 1,
    int PageSize = 20) : PaginationParams(PageNumber, PageSize);
