using System.Linq;
using Hartonomous.Core.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Hartonomous.Infrastructure.Extensions;

/// <summary>
/// Extension methods for applying Specification pattern to IQueryable.
/// Provides fluent API for building complex queries from specifications.
/// </summary>
public static class SpecificationExtensions
{
    /// <summary>
    /// Apply a specification to a queryable, including filters, includes, ordering, and paging.
    /// </summary>
    public static IQueryable<T> ApplySpecification<T>(
        this IQueryable<T> query,
        ISpecification<T> specification) where T : class
    {
        // Apply criteria
        if (specification.Criteria != null)
        {
            query = query.Where(specification.Criteria);
        }

        // Apply includes
        query = specification.Includes
            .Aggregate(query, (current, include) => current.Include(include));

        // Apply string-based includes
        query = specification.IncludeStrings
            .Aggregate(query, (current, include) => current.Include(include));

        // Apply ordering
        if (specification.OrderBy != null)
        {
            query = query.OrderBy(specification.OrderBy);
        }
        else if (specification.OrderByDescending != null)
        {
            query = query.OrderByDescending(specification.OrderByDescending);
        }

        // Apply paging
        if (specification.Skip.HasValue)
        {
            query = query.Skip(specification.Skip.Value);
        }

        if (specification.Take.HasValue)
        {
            query = query.Take(specification.Take.Value);
        }

        return query;
    }
}
