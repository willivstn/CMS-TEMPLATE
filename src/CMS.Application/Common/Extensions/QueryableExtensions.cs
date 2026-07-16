using CMS.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace CMS.Application.Common.Extensions;

/// <summary>Query helpers shared across the Application layer.</summary>
public static class QueryableExtensions
{
    private const int MaxPageSize = 200;

    /// <summary>
    /// Materialises a page of results with a total count in a single round-trip
    /// shape (count + page). Page number and size are clamped to safe bounds.
    /// </summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        pageNumber = pageNumber < 1 ? 1 : pageNumber;
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(items, totalCount, pageNumber, pageSize);
    }
}
