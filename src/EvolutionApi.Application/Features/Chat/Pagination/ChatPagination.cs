using EvolutionApi.Application.Common.Pagination;
using System.Text.Json;

namespace EvolutionApi.Application.Features.Chat.Pagination;

/// <summary>
/// Pagination configuration and helpers for Chat.
/// </summary>
public sealed record ChatPaginationParams(
    string? RemoteJid = null,
    int PageNumber = 1,
    int PageSize = 20) : PaginationParams(PageNumber, PageSize);

public static class ChatPaginationExtensions
{
    public static JsonElement PaginateChatItems(this JsonElement items, int pageNumber, int pageSize)
        => JsonPaginationHelper.Paginate(items, pageNumber, pageSize);
}
