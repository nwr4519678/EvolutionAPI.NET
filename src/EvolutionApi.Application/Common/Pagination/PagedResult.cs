namespace EvolutionApi.Application.Common.Pagination;

/// <summary>
/// Generic paginated response envelope with metadata.
/// </summary>
public sealed record PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public PagedResult() { }

    public PagedResult(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber < 1 ? 1 : pageNumber;
        PageSize = pageSize < 1 ? 20 : pageSize;
    }

    public static PagedResult<T> Create(IEnumerable<T> source, int pageNumber, int pageSize)
    {
        var list = source as IReadOnlyList<T> ?? source.ToList();
        var count = list.Count;
        var page = pageNumber < 1 ? 1 : pageNumber;
        var size = pageSize < 1 ? 20 : pageSize;
        var items = list.Skip((page - 1) * size).Take(size).ToList();
        return new PagedResult<T>(items, count, page, size);
    }
}
