using System.Linq.Expressions;
using Infrastructure.Repository.Interfaces;

namespace Infrastructure.Repository.Specification;

/// <summary>
/// Composite specification that combines two specifications using AND logic.
/// Both specifications' criteria must be satisfied for an entity to match.
/// </summary>
/// <typeparam name="TEntity">The entity type being queried.</typeparam>
/// <remarks>
/// <para>
/// This specification combines two specifications using AND logic:
/// <list type="bullet">
/// <item><description>Criteria are combined with AND (both must be true)</description></item>
/// <item><description>Includes are merged from both specifications</description></item>
/// <item><description>Ordering uses the left specification's preference</description></item>
/// <item><description>Paging uses the left specification's preference</description></item>
/// <item><description>Tracking preference: tracking is enabled if either specification wants it</description></item>
/// </list>
/// </para>
/// 
/// <para>
/// Usage example:
/// <code>
/// var activeSpec = new ActiveCustomersSpecification();
/// var inRegionSpec = new CustomersInRegionSpecification(regionId);
/// var combinedSpec = new AndSpecification&lt;Customer&gt;(activeSpec, inRegionSpec);
/// 
/// var results = await repository.FindAsync(combinedSpec, QueryOptions.Default);
/// // Returns customers that are both active AND in the specified region
/// </code>
/// </para>
/// </remarks>
public class AndSpecification<TEntity> : BaseSpecification<TEntity> where TEntity : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AndSpecification{TEntity}"/> class.
    /// </summary>
    /// <param name="left">The left specification to combine.</param>
    /// <param name="right">The right specification to combine.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="left"/> or <paramref name="right"/> is null.
    /// </exception>
    public AndSpecification(ISpecification<TEntity> left, ISpecification<TEntity> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        // Combine criteria with AND logic
        if (left.Criteria != null && right.Criteria != null)
        {
            Criteria = CombineExpressions(left.Criteria, right.Criteria, Expression.AndAlso);
        }
        else if (left.Criteria != null)
        {
            Criteria = left.Criteria;
        }
        else if (right.Criteria != null)
        {
            Criteria = right.Criteria;
        }

        // Merge includes from both specifications
        if (left.Includes != null)
            Includes.AddRange(left.Includes);
        if (right.Includes != null)
            Includes.AddRange(right.Includes);
        
        if (left.IncludeStrings != null)
            IncludeStrings.AddRange(left.IncludeStrings);
        if (right.IncludeStrings != null)
            IncludeStrings.AddRange(right.IncludeStrings);

        // Use left specification's ordering preference
        OrderBy = left.OrderBy ?? right.OrderBy;
        OrderByDescending = left.OrderByDescending ?? right.OrderByDescending;

        // Use left specification's paging preference
        Skip = left.Skip ?? right.Skip;
        Take = left.Take ?? right.Take;

        // Tracking: enable if either specification wants tracking
        // AsNoTracking = false means tracking is enabled
        AsNoTracking = left.AsNoTracking && right.AsNoTracking;
    }

    /// <summary>
    /// Combines two boolean expressions using the specified binary expression type.
    /// </summary>
    private static Expression<Func<TEntity, bool>> CombineExpressions(
        Expression<Func<TEntity, bool>> left,
        Expression<Func<TEntity, bool>> right,
        Func<Expression, Expression, BinaryExpression> combinator)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        ArgumentNullException.ThrowIfNull(combinator);
        
        var parameter = Expression.Parameter(typeof(TEntity));
        var leftVisitor = new ReplaceExpressionVisitor(left.Parameters[0], parameter);
        var leftExpression = leftVisitor.Visit(left.Body);
        var rightVisitor = new ReplaceExpressionVisitor(right.Parameters[0], parameter);
        var rightExpression = rightVisitor.Visit(right.Body);
        
        return Expression.Lambda<Func<TEntity, bool>>(
            combinator(leftExpression, rightExpression),
            parameter);
    }

    private class ReplaceExpressionVisitor : ExpressionVisitor
    {
        private readonly Expression _oldValue;
        private readonly Expression _newValue;

        public ReplaceExpressionVisitor(Expression oldValue, Expression newValue)
        {
            _oldValue = oldValue;
            _newValue = newValue;
        }

        public override Expression Visit(Expression? node)
        {
            if (node == null)
                return base.Visit(node)!;
            
            return node == _oldValue ? _newValue : base.Visit(node)!;
        }
    }
}

