using EvolutionApi.Application.Common.Pagination;
using EvolutionApi.Application.Features.Instances.Queries.GetInstances;

namespace EvolutionApi.Application.Features.Instances.Pagination;

/// <summary>
/// Pagination parameters and extensions for Instances.
/// </summary>
public sealed record InstancePaginationParams(
    string? InstanceName = null,
    int PageNumber = 1,
    int PageSize = 20) : PaginationParams(PageNumber, PageSize);

public static class InstancePaginationExtensions
{
    public static PagedResult<InstanceDto> ToPagedResult(
        this IEnumerable<InstanceDto> source,
        int pageNumber,
        int pageSize)
        => PagedResult<InstanceDto>.Create(source, pageNumber, pageSize);
}
