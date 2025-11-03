using Infrastructure.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository.Specification;

/// <summary>
/// Static utility class for applying specifications to IQueryable queries.
/// Transforms a base query by applying criteria, includes, ordering, and paging.
/// </summary>
/// <remarks>
/// This class takes a specification and applies all its components to a query:
    /// <list type="number">
    /// <item><description>Applies filtering criteria (Where clause)</description></item>
    /// <item><description>Applies includes for eager loading related entities</description></item>
    /// <item><description>Applies ordering (OrderBy or OrderByDescending)</description></item>
    /// <item><description>Applies paging (Skip and Take)</description></item>
    /// </list>
/// 
/// This evaluator is used internally by repositories to apply specifications to queries
/// before execution. It ensures specifications are consistently applied across all repository methods.
/// </remarks>
public static class SpecificationEvaluator
{
    /// <summary>
    /// Applies a specification to an IQueryable query, returning a modified query.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being queried.</typeparam>
    /// <param name="inputQuery">The base query to apply the specification to.</param>
    /// <param name="spec">The specification containing criteria, includes, ordering, and paging.</param>
    /// <returns>A modified query with the specification applied.</returns>
    /// <remarks>
    /// The specification is applied in the following order:
    /// <list type="number">
    /// <item><description>Where clause (if criteria is specified)</description></item>
    /// <item><description>Includes (both expression-based and string-based)</description></item>
    /// <item><description>Ordering (OrderBy takes precedence over OrderByDescending)</description></item>
    /// <item><description>Paging (Skip and Take)</description></item>
    /// </list>
    /// 
    /// <para>
    /// Note: Tracking is not applied by this evaluator. The <see cref="ISpecification{TEntity}.AsNoTracking"/>
    /// property is used by the repository when determining tracking preferences. The repository applies
    /// tracking based on the following priority:
    /// <list type="number">
    /// <item><description>If <see cref="QueryOptions.TrackChanges"/> is <c>true</c>, tracking is enabled</description></item>
    /// <item><description>If <see cref="QueryOptions"/> is <see cref="QueryOptions.Default"/> and a specification is provided, the specification's <see cref="ISpecification{TEntity}.AsNoTracking"/> preference is used</description></item>
    /// <item><description>Otherwise, no tracking is used (default for read-only performance)</description></item>
    /// </list>
    /// This ensures explicit <see cref="QueryOptions"/> settings take precedence, while specifications
    /// can express their tracking intent when using default query options.
    /// </para>
    /// 
    /// <para>
    /// When used with <see cref="IRepository{TEntity}.GetByIdAsync"/> methods,
    /// the criteria from the specification is redundant since ID filtering is applied separately,
    /// but includes are still applied correctly.
    /// </para>
    /// </remarks>
    public static IQueryable<TEntity> GetQuery<TEntity>(
        IQueryable<TEntity> inputQuery,
        ISpecification<TEntity> spec) where TEntity : class
    {
        var query = inputQuery;

        if (spec.Criteria != null)
            query = query.Where(spec.Criteria);

        query = spec.Includes.Aggregate(query, (current, include) => current.Include(include));
        query = spec.IncludeStrings.Aggregate(query, (current, include) => current.Include(include));

        if (spec.OrderBy != null)
            query = query.OrderBy(spec.OrderBy);
        else if (spec.OrderByDescending != null)
            query = query.OrderByDescending(spec.OrderByDescending);

        if (spec.Skip.HasValue)
            query = query.Skip(spec.Skip.Value);

        if (spec.Take.HasValue)
            query = query.Take(spec.Take.Value);

        // Note: Tracking is not applied here by the evaluator.
        // The repository's ApplyTracking method will use the specification's AsNoTracking
        // property when QueryOptions is Default, allowing specifications to express
        // their tracking intent (e.g., via BaseSpecification.ApplyTracking()).

        return query;
    }
}