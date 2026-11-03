using System.Text.Json;

namespace EvolutionApi.Application.Common.Pagination;

/// <summary>
/// Helper for paginating raw JsonElement arrays returned from engine calls.
/// </summary>
public static class JsonPaginationHelper
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public static JsonElement Paginate(JsonElement root, int pageNumber, int pageSize)
    {
        if (root.ValueKind != JsonValueKind.Array)
            return root;

        var items = root.EnumerateArray().ToList();
        var totalCount = items.Count;
        var page = pageNumber < 1 ? 1 : pageNumber;
        var size = pageSize < 1 ? 20 : pageSize;
        var pagedItems = items.Skip((page - 1) * size).Take(size).ToList();
        var totalPages = size > 0 ? (int)Math.Ceiling(totalCount / (double)size) : 0;

        var resultObj = new
        {
            items = pagedItems,
            pageNumber = page,
            pageSize = size,
            totalCount = totalCount,
            totalPages = totalPages,
            hasPreviousPage = page > 1,
            hasNextPage = page < totalPages
        };

        return JsonSerializer.SerializeToElement(resultObj, JsonOpts);
    }
}
