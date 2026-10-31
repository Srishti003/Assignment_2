namespace BankingLedger.Application.Common;

/// <summary>
/// Parameters for pagination requests.
/// 
/// Business Context:
/// When an account holds thousands of transactions, querying the entire history at once
/// degrades database performance and wastes mobile/web client bandwidth.
/// Pagination breaks large result sets into bite-sized, fast-loading chunks.
/// </summary>
public record PaginationParams
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 10;

    public int Page { get; init; } = 1;
    public int Limit { get; init; } = DefaultPageSize;

    /// <summary>
    /// Calculated number of records to skip in SQL OFFSET queries.
    /// </summary>
    public int Skip => (Page - 1) * Limit;

    /// <summary>
    /// Clamps the page limit so an aggressive client cannot request 1,000,000 records at once.
    /// </summary>
    public int SafeLimit => Limit > MaxPageSize ? MaxPageSize : (Limit <= 0 ? DefaultPageSize : Limit);
}

/// <summary>
/// Generic paginated response metadata.
/// 
/// Returns everything the frontend or API consumer needs to render pagination buttons:
/// - "Page 2 of 5"
/// - "Showing 10 records per page"
/// - "Total 48 transactions found"
/// </summary>
public record PagedResult<T>
{
    public int CurrentPage { get; init; }
    public int PageSize { get; init; }
    public int TotalRecords { get; init; }
    public int TotalPages { get; init; }
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

    public static PagedResult<T> Create(IReadOnlyList<T> items, int totalRecords, int currentPage, int pageSize)
    {
        var totalPages = pageSize > 0 ? (int)Math.Ceiling(totalRecords / (double)pageSize) : 0;

        return new PagedResult<T>
        {
            CurrentPage = currentPage,
            PageSize = pageSize,
            TotalRecords = totalRecords,
            TotalPages = totalPages,
            Items = items
        };
    }
}
