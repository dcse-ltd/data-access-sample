namespace Infrastructure.Repository.Models;

/// <summary>
/// Represents a paginated result set with metadata about the pagination.
/// </summary>
/// <typeparam name="T">The type of items in the result set.</typeparam>
/// <remarks>
/// This type provides comprehensive pagination information including:
/// <list type="bullet">
/// <item><description>The items for the current page</description></item>
/// <item><description>Total count of items across all pages</description></item>
/// <item><description>Current page number and page size</description></item>
/// <item><description>Calculated total pages</description></item>
/// <item><description>Boolean flags indicating if previous/next pages exist</description></item>
/// </list>
/// 
/// Usage example:
/// <code>
/// var spec = new FindCustomersSpecification(lastName: "Smith", page: 1, pageSize: 10);
/// var result = await repository.FindPagedAsync(spec, QueryOptions.Default);
/// 
/// Console.WriteLine($"Showing {result.Items.Count()} of {result.TotalCount} results");
/// Console.WriteLine($"Page {result.Page} of {result.TotalPages}");
/// 
/// if (result.HasNextPage)
///     Console.WriteLine("More results available");
/// </code>
/// </remarks>
public class PagedResult<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PagedResult{T}"/> class.
    /// </summary>
    /// <param name="items">The items for the current page.</param>
    /// <param name="totalCount">The total count of items across all pages.</param>
    /// <param name="page">The current page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="page"/> is less than 1 or <paramref name="pageSize"/> is less than 1.
    /// </exception>
    public PagedResult(
        IEnumerable<T> items,
        int totalCount,
        int page,
        int pageSize)
    {
        if (page < 1)
            throw new ArgumentOutOfRangeException(nameof(page), "Page must be greater than or equal to 1.");
        if (pageSize < 1)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than or equal to 1.");

        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }

    /// <summary>
    /// Gets the items for the current page.
    /// </summary>
    public IEnumerable<T> Items { get; }

    /// <summary>
    /// Gets the total count of items across all pages.
    /// </summary>
    public int TotalCount { get; }

    /// <summary>
    /// Gets the current page number (1-based).
    /// </summary>
    public int Page { get; }

    /// <summary>
    /// Gets the number of items per page.
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    /// <remarks>
    /// Calculated as the ceiling of <see cref="TotalCount"/> divided by <see cref="PageSize"/>.
    /// If <see cref="TotalCount"/> is 0, returns 0.
    /// </remarks>
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// Gets a value indicating whether there is a previous page.
    /// </summary>
    /// <returns>True if the current page is greater than 1; otherwise, false.</returns>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Gets a value indicating whether there is a next page.
    /// </summary>
    /// <returns>True if the current page is less than <see cref="TotalPages"/>; otherwise, false.</returns>
    public bool HasNextPage => Page < TotalPages;
}

