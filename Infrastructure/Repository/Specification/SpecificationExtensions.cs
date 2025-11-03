using Infrastructure.Repository.Interfaces;

namespace Infrastructure.Repository.Specification;

/// <summary>
/// Extension methods for combining specifications using AND and OR logic.
/// </summary>
public static class SpecificationExtensions
{
    /// <summary>
    /// Combines this specification with another using AND logic.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being queried.</typeparam>
    /// <param name="left">The left specification.</param>
    /// <param name="right">The right specification to combine with.</param>
    /// <returns>A new <see cref="AndSpecification{TEntity}"/> that combines both specifications.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="left"/> or <paramref name="right"/> is null.
    /// </exception>
    /// <remarks>
    /// This method provides a fluent API for combining specifications:
    /// <code>
    /// var spec = new ActiveCustomersSpecification()
    ///     .And(new CustomersInRegionSpecification(regionId));
    /// </code>
    /// </remarks>
    public static AndSpecification<TEntity> And<TEntity>(
        this ISpecification<TEntity> left,
        ISpecification<TEntity> right)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        
        return new AndSpecification<TEntity>(left, right);
    }

    /// <summary>
    /// Combines this specification with another using OR logic.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being queried.</typeparam>
    /// <param name="left">The left specification.</param>
    /// <param name="right">The right specification to combine with.</param>
    /// <returns>A new <see cref="OrSpecification{TEntity}"/> that combines both specifications.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="left"/> or <paramref name="right"/> is null.
    /// </exception>
    /// <remarks>
    /// This method provides a fluent API for combining specifications:
    /// <code>
    /// var spec = new ActiveCustomersSpecification()
    ///     .Or(new VipCustomersSpecification());
    /// </code>
    /// </remarks>
    public static OrSpecification<TEntity> Or<TEntity>(
        this ISpecification<TEntity> left,
        ISpecification<TEntity> right)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        
        return new OrSpecification<TEntity>(left, right);
    }
}

