namespace EvolutionApi.Application.Common.Pagination;

/// <summary>
/// Common request parameters for paginated queries.
/// </summary>
public record PaginationParams(int PageNumber = 1, int PageSize = 20)
{
    public const int DefaultPageNumber = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public int PageNumber { get; init; } = PageNumber < 1 ? DefaultPageNumber : PageNumber;
    public int PageSize { get; init; } = PageSize < 1 ? DefaultPageSize : (PageSize > MaxPageSize ? MaxPageSize : PageSize);
}
