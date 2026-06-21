using System.Collections.Generic;

namespace Application.Shared;

/// <summary>
/// A standardized wrapper for returning paginated API responses using a cursor.
/// </summary>
/// <typeparam name="T">The type of the DTO being returned.</typeparam>
/// <param name="Items">The actual list of plans for this page</param>
/// <param name="NextCursor">The ID the frontend needs to pass to fetch the next page</param>
/// <param name="HasNextPage">True if there is more data available</param>
public record CursorPagedResult<T>(
    IEnumerable<T> Items,
    string? NextCursor,
    bool HasNextPage
);



