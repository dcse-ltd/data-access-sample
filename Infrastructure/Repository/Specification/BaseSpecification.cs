using System.Linq.Expressions;
using Infrastructure.Repository.Interfaces;

namespace Infrastructure.Repository.Specification;

/// <summary>
/// Base class for implementing specifications that encapsulate query logic.
/// Provides helper methods for building specifications with criteria, includes, ordering, and paging.
/// </summary>
/// <typeparam name="TEntity">The entity type being queried.</typeparam>
/// <remarks>
/// This abstract class provides a foundation for implementing the specification pattern.
/// It simplifies creating specifications by providing protected helper methods:
/// <list type="bullet">
/// <item><description><see cref="AddCriteria"/> - Add filtering criteria</description></item>
/// <item><description><see cref="AddInclude"/> - Include related entities using expressions</description></item>
/// <item><description><see cref="AddInclude(string)"/> - Include related entities using string paths</description></item>
/// <item><description><see cref="ApplyOrderBy"/> - Apply ascending ordering</description></item>
/// <item><description><see cref="ApplyOrderByDescending"/> - Apply descending ordering</description></item>
/// <item><description><see cref="ApplyPaging"/> - Apply skip and take for pagination</description></item>
/// <item><description><see cref="ApplyTracking"/> - Enable change tracking (disables AsNoTracking)</description></item>
/// </list>
/// 
/// <para>
/// Specifications can be combined using composite specifications:
/// <list type="bullet">
/// <item><description><see cref="AndSpecification{TEntity}"/> - Combines two specifications with AND logic</description></item>
/// <item><description><see cref="OrSpecification{TEntity}"/> - Combines two specifications with OR logic</description></item>
/// </list>
/// 
/// Use extension methods for a fluent API:
/// <code>
/// var spec = new ActiveCustomersSpecification()
///     .And(new CustomersInRegionSpecification(regionId));
/// </code>
/// </para>
/// 
/// Usage example:
/// <code>
/// public class OrderWithProductsSpecification : BaseSpecification&lt;Order&gt;
/// {
///     public OrderWithProductsSpecification(Guid customerId)
///     {
///         AddCriteria(order => order.CustomerId == customerId);
///         AddInclude(order => order.OrderProducts);
///         ApplyOrderBy(order => order.Date);
///         ApplyTracking(); // Enable tracking for updates
///     }
/// }
/// 
/// public class FindCustomersSpecification : BaseSpecification&lt;Customer&gt;
/// {
///     public FindCustomersSpecification(string lastName, int page = 1, int pageSize = 10)
///     {
///         if (!string.IsNullOrEmpty(lastName))
///             AddCriteria(customer => customer.LastName.StartsWith(lastName));
///         
///         AddInclude(customer => customer.Orders);
///         ApplyOrderBy(customer => customer.LastName);
///         ApplyPaging((page - 1) * pageSize, pageSize);
///     }
/// }
/// </code>
/// </remarks>
public abstract class BaseSpecification<TEntity> : ISpecification<TEntity> where TEntity : class
{
    public Expression<Func<TEntity, bool>>? Criteria { get; protected set; }
    public List<Expression<Func<TEntity, object>>> Includes { get; } = [];
    public List<string> IncludeStrings { get; } = [];
    public Expression<Func<TEntity, object>>? OrderBy { get; protected set; }
    public Expression<Func<TEntity, object>>? OrderByDescending { get; protected set; }
    public int? Skip { get; protected set; }
    public int? Take { get; protected set; }
    public bool AsNoTracking { get; protected set; } = true;

    /// <summary>
    /// Adds a filtering criteria expression to the specification.
    /// </summary>
    /// <param name="criteria">The LINQ expression defining the filter condition.</param>
    /// <remarks>
    /// If criteria already exists, the new criteria will be combined with AND logic.
    /// To combine multiple conditions with AND, use <see cref="AddCriteriaAnd"/>.
    /// To combine with OR logic, use <see cref="AddCriteriaOr"/>.
    /// </remarks>
    protected void AddCriteria(Expression<Func<TEntity, bool>> criteria)
    {
        if (Criteria == null)
        {
            Criteria = criteria;
        }
        else
        {
            Criteria = CombineExpressions(Criteria, criteria, Expression.AndAlso);
        }
    }

    /// <summary>
    /// Adds a filtering criteria expression that will be combined with existing criteria using AND logic.
    /// </summary>
    /// <param name="criteria">The LINQ expression defining the filter condition.</param>
    /// <remarks>
    /// This method is equivalent to <see cref="AddCriteria"/> but makes the AND combination explicit.
    /// </remarks>
    protected void AddCriteriaAnd(Expression<Func<TEntity, bool>> criteria)
        => AddCriteria(criteria);

    /// <summary>
    /// Adds a filtering criteria expression that will be combined with existing criteria using OR logic.
    /// </summary>
    /// <param name="criteria">The LINQ expression defining the filter condition.</param>
    /// <remarks>
    /// If no criteria exists yet, this simply sets the criteria (same as <see cref="AddCriteria"/>).
    /// </remarks>
    protected void AddCriteriaOr(Expression<Func<TEntity, bool>> criteria)
    {
        if (Criteria == null)
        {
            Criteria = criteria;
        }
        else
        {
            Criteria = CombineExpressions(Criteria, criteria, Expression.OrElse);
        }
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

    /// <summary>
    /// Adds a strongly-typed include expression for eager loading a related entity.
    /// </summary>
    /// <param name="includeExpression">The LINQ expression specifying the related entity to include.</param>
    /// <remarks>
    /// Use this method to include related entities using strongly-typed expressions
    /// (e.g., <c>AddInclude(order => order.OrderProducts)</c>).
    /// Multiple includes can be added to load multiple related entities.
    /// </remarks>
    protected void AddInclude(Expression<Func<TEntity, object>> includeExpression) 
        => Includes.Add(includeExpression);

    /// <summary>
    /// Adds a string-based include path for eager loading a related entity.
    /// </summary>
    /// <param name="includeString">The string path specifying the related entity to include.</param>
    /// <remarks>
    /// Use this method when you need dynamic includes or when navigating complex relationship paths
    /// (e.g., <c>AddInclude("OrderProducts.Product")</c>).
    /// Multiple includes can be added to load multiple related entities.
    /// </remarks>
    protected void AddInclude(string includeString) 
        => IncludeStrings.Add(includeString);

    /// <summary>
    /// Applies ascending ordering to the query results.
    /// </summary>
    /// <param name="orderByExpression">The LINQ expression specifying the property to order by.</param>
    /// <remarks>
    /// If both OrderBy and OrderByDescending are applied, OrderBy takes precedence.
    /// Multiple calls will overwrite the previous ordering.
    /// </remarks>
    protected void ApplyOrderBy(Expression<Func<TEntity, object>> orderByExpression) 
        => OrderBy = orderByExpression;

    /// <summary>
    /// Applies descending ordering to the query results.
    /// </summary>
    /// <param name="orderByDescExpression">The LINQ expression specifying the property to order by.</param>
    /// <remarks>
    /// Used only if OrderBy has not been set. Multiple calls will overwrite the previous ordering.
    /// </remarks>
    protected void ApplyOrderByDescending(Expression<Func<TEntity, object>> orderByDescExpression) 
        => OrderByDescending = orderByDescExpression;

    /// <summary>
    /// Applies pagination to the query results.
    /// </summary>
    /// <param name="skip">The number of entities to skip.</param>
    /// <param name="take">The number of entities to take.</param>
    /// <remarks>
    /// Use this method to implement pagination. The skip value is typically calculated as
    /// <c>(page - 1) * pageSize</c>, where page is 1-based.
    /// </remarks>
    protected void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
    }

    /// <summary>
    /// Enables change tracking for entities returned by queries using this specification.
    /// </summary>
    /// <remarks>
    /// By default, specifications use AsNoTracking for better read-only performance.
    /// Call this method when you need to track entities for modifications (e.g., in update operations).
    /// Note: This may be overridden by <see cref="QueryOptions.TrackChanges"/>.
    /// </remarks>
    protected void ApplyTracking() 
        => AsNoTracking = false;
}