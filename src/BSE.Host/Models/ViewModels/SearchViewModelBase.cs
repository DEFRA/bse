namespace BSE.Host.Models.ViewModels;

/// <summary>
/// Shared pagination and sorting base for all search view-models.
/// Concrete sub-classes provide the page size constant, sort logic, and filter properties.
/// </summary>
public abstract class SearchViewModelBase<TResult>
{
    // --- Pagination ---
    public int PageNumber { get; set; } = 1;

    // --- Sorting ---
    public string SortColumn { get; set; } = "";
    public bool SortDesc { get; set; }

    // --- Results ---
    public IReadOnlyList<TResult> Results { get; set; } = [];
    public bool HasSearched { get; set; }

    protected abstract int PageSize { get; }

    public int TotalCount => Results.Count;
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public IReadOnlyList<TResult> PagedResults =>
        ApplySorting(Results)
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToList();

    /// <summary>Full sorted result set, unpaged — Excel export must include every matching row
    /// in the same order as the on-screen grid, not just the current page.</summary>
    public IReadOnlyList<TResult> SortedResults => ApplySorting(Results).ToList();

    /// <summary>Apply sorting to the full result set before paging.</summary>
    protected abstract IEnumerable<TResult> ApplySorting(IReadOnlyList<TResult> source);
}
