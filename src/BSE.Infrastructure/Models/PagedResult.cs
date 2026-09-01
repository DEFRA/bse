namespace BSE.Infrastructure.Models;

/// <summary>
/// A lightweight wrapper for a page of results from an in-memory or DB-paged query.
/// </summary>
public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }

    public int TotalPages => PageSize > 0
        ? (int)Math.Ceiling((double)TotalCount / PageSize)
        : 0;

    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
