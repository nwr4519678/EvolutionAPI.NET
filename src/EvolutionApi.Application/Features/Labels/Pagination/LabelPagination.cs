using EvolutionApi.Application.Common.Pagination;
using System.Text.Json;

namespace EvolutionApi.Application.Features.Labels.Pagination;

/// <summary>
/// Pagination parameters and helpers for Labels.
/// </summary>
public sealed record LabelPaginationParams(
    string InstanceName = "",
    int PageNumber = 1,
    int PageSize = 20) : PaginationParams(PageNumber, PageSize);

public static class LabelPaginationExtensions
{
    public static JsonElement PaginateLabels(this JsonElement labels, int pageNumber, int pageSize)
        => JsonPaginationHelper.Paginate(labels, pageNumber, pageSize);
}
