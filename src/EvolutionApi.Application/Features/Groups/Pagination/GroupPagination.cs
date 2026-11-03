using EvolutionApi.Application.Common.Pagination;
using System.Text.Json;

namespace EvolutionApi.Application.Features.Groups.Pagination;

/// <summary>
/// Pagination parameters and helpers for Groups.
/// </summary>
public sealed record GroupPaginationParams(
    string InstanceName = "",
    int PageNumber = 1,
    int PageSize = 20) : PaginationParams(PageNumber, PageSize);

public static class GroupPaginationExtensions
{
    public static JsonElement PaginateGroups(this JsonElement groups, int pageNumber, int pageSize)
        => JsonPaginationHelper.Paginate(groups, pageNumber, pageSize);
}
