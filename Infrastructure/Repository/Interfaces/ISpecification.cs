using System.Linq.Expressions;
using Infrastructure.Repository.Specification;

namespace Infrastructure.Repository.Interfaces;

/// <summary>
/// Specification interface defining a query pattern for entities.
/// Provides a way to encapsulate query logic including criteria, includes, ordering, and paging.
/// </summary>
/// <typeparam name="TEntity">The entity type being queried.</typeparam>
/// <remarks>
/// The specification pattern allows query logic to be encapsulated and reused across the application.
/// It supports:
/// <list type="bullet">
/// <item><description>Criteria - filtering conditions using LINQ expressions</description></item>
/// <item><description>Includes - eager loading of related entities using LINQ expressions</description></item>
/// <item><description>IncludeStrings - eager loading of related entities using string paths</description></item>
/// <item><description>Ordering - ascending or descending ordering</description></item>
/// <item><description>Paging - skip and take for pagination</description></item>
/// <item><description>Tracking - whether to track entities for change detection</description></item>
/// </list>
/// 
/// Specifications are typically created by inheriting from <see cref="BaseSpecification{TEntity}"/>,
/// which provides helper methods for building specifications.
/// 
/// Specifications can be combined using composite specifications (<see cref="AndSpecification{TEntity}"/>,
/// <see cref="OrSpecification{TEntity}"/>) or extension methods for a fluent API.
/// 
/// Usage example:
/// <code>
/// // Single specification
/// public class OrderWithProductsSpecification : BaseSpecification&lt;Order&gt;
/// {
///     public OrderWithProductsSpecification()
///     {
///         AddInclude(order => order.OrderProducts);
///         ApplyTracking(); // Enable tracking for updates
///     }
/// }
/// 
/// // Composite specifications
/// var activeSpec = new ActiveCustomersSpecification();
/// var inRegionSpec = new CustomersInRegionSpecification(regionId);
/// var combinedSpec = activeSpec.And(inRegionSpec); // Fluent API
/// // or
/// var combinedSpec = new AndSpecification&lt;Customer&gt;(activeSpec, inRegionSpec);
/// </code>
/// </remarks>
public interface ISpecification<TEntity> where TEntity : class
{
    /// <summary>
    /// Gets the filtering criteria expression for the query.
    /// </summary>
    /// <remarks>
    /// If null, no additional filtering is applied beyond the base query.
    /// </remarks>
    Expression<Func<TEntity, bool>>? Criteria { get; }
    
    /// <summary>
    /// Gets the list of include expressions for eager loading related entities.
    /// </summary>
    /// <remarks>
    /// These expressions are used to include related entities using strongly-typed LINQ expressions
    /// (e.g., <c>order => order.OrderProducts</c>).
    /// </remarks>
    List<Expression<Func<TEntity, object>>> Includes { get; }
    
    /// <summary>
    /// Gets the list of include string paths for eager loading related entities.
    /// </summary>
    /// <remarks>
    /// These strings are used to include related entities using string-based paths
    /// (e.g., "OrderProducts" or "OrderProducts.Product").
    /// Useful when dynamic includes are needed or when navigating complex relationships.
    /// </remarks>
    List<string> IncludeStrings { get; }
    
    /// <summary>
    /// Gets the ordering expression for ascending sort.
    /// </summary>
    /// <remarks>
    /// If both <see cref="OrderBy"/> and <see cref="OrderByDescending"/> are specified,
    /// <see cref="OrderBy"/> takes precedence.
    /// </remarks>
    Expression<Func<TEntity, object>>? OrderBy { get; }
    
    /// <summary>
    /// Gets the ordering expression for descending sort.
    /// </summary>
    /// <remarks>
    /// Used only if <see cref="OrderBy"/> is null.
    /// </remarks>
    Expression<Func<TEntity, object>>? OrderByDescending { get; }
    
    /// <summary>
    /// Gets the number of entities to skip for pagination.
    /// </summary>
    /// <remarks>
    /// Used for implementing pagination. Typically combined with <see cref="Take"/>.
    /// </remarks>
    int? Skip { get; }
    
    /// <summary>
    /// Gets the number of entities to take for pagination.
    /// </summary>
    /// <remarks>
    /// Used for implementing pagination. Typically combined with <see cref="Skip"/>.
    /// </remarks>
    int? Take { get; }
    
    /// <summary>
    /// Gets whether the query should use AsNoTracking (disable change tracking).
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>true</c>, entities returned from queries will not be tracked by the change tracker.
    /// This improves performance for read-only operations but prevents automatic change detection.
    /// </para>
    /// <para>
    /// This property is used by the repository when determining tracking preferences:
    /// <list type="number">
    /// <item><description>If <see cref="QueryOptions.TrackChanges"/> is <c>true</c>, tracking is always enabled (this property is ignored)</description></item>
    /// <item><description>If <see cref="QueryOptions"/> is <see cref="QueryOptions.Default"/>, this property's value is used</description></item>
    /// <item><description>Otherwise, no tracking is used by default</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Use <see cref="BaseSpecification{TEntity}.ApplyTracking"/> in your specification constructor
    /// to enable tracking for specifications that need it (e.g., for update operations).
    /// </para>
    /// </remarks>
    bool AsNoTracking { get; }
}