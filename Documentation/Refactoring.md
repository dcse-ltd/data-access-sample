## 1. Specification Pattern

Implement specifications that work with `IQueryable`:

```csharp
public interface ISpecification<TEntity> where TEntity : class
{
    Expression<Func<TEntity, bool>>? Criteria { get; }
    List<Expression<Func<TEntity, object>>> Includes { get; }
    List<string> IncludeStrings { get; }
    Expression<Func<TEntity, object>>? OrderBy { get; }
    Expression<Func<TEntity, object>>? OrderByDescending { get; }
    int? Skip { get; }
    int? Take { get; }
    bool AsNoTracking { get; }
}

public abstract class BaseSpecification<TEntity> : ISpecification<TEntity> where TEntity : class
{
    public Expression<Func<TEntity, bool>>? Criteria { get; private set; }
    public List<Expression<Func<TEntity, object>>> Includes { get; } = new();
    public List<string> IncludeStrings { get; } = new();
    public Expression<Func<TEntity, object>>? OrderBy { get; private set; }
    public Expression<Func<TEntity, object>>? OrderByDescending { get; private set; }
    public int? Skip { get; private set; }
    public int? Take { get; private set; }
    public bool AsNoTracking { get; private set; } = true;

    protected void AddCriteria(Expression<Func<TEntity, bool>> criteria) 
        => Criteria = criteria;

    protected void AddInclude(Expression<Func<TEntity, object>> includeExpression) 
        => Includes.Add(includeExpression);

    protected void AddInclude(string includeString) 
        => IncludeStrings.Add(includeString);

    protected void ApplyOrderBy(Expression<Func<TEntity, object>> orderByExpression) 
        => OrderBy = orderByExpression;

    protected void ApplyOrderByDescending(Expression<Func<TEntity, object>> orderByDescExpression) 
        => OrderByDescending = orderByDescExpression;

    protected void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
    }

    protected void ApplyTracking() 
        => AsNoTracking = false;
}
```

Extend the repository:

```csharp
public interface IRepository<TEntity> where TEntity : class, IEntity
{
    // Existing methods...
    Task<IEnumerable<TEntity>> FindAsync(ISpecification<TEntity> spec, CancellationToken cancellationToken = default);
    Task<TEntity?> FindOneAsync(ISpecification<TEntity> spec, CancellationToken cancellationToken = default);
    Task<int> CountAsync(ISpecification<TEntity> spec, CancellationToken cancellationToken = default);
}
```

Add a specification evaluator:

```csharp
public static class SpecificationEvaluator
{
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

        if (spec.AsNoTracking)
            query = query.AsNoTracking();

        return query;
    }
}
```

Example usage:

```csharp
public class ActiveCustomersWithOrdersSpec : BaseSpecification<Customer>
{
    public ActiveCustomersWithOrdersSpec(int page, int pageSize)
    {
        AddCriteria(c => c.IsActive);
        AddInclude(c => c.Orders);
        AddOrderBy(c => c.Name);
        ApplyPaging((page - 1) * pageSize, pageSize);
    }
}

// In service
var spec = new ActiveCustomersWithOrdersSpec(page: 1, pageSize: 20);
var customers = await repository.FindAsync(spec, cancellationToken);
```

## 2. **Batch Operations - GetByIdsAsync**
## 3. **Add EF Annotations for Entity Fields/Properties**
## 4. **Consider explicit index configuration in entity configurations**
## 5. **Entity Collection Comparer (Make Generic)**

Recommended Approach
Instead of mapping the entire collection, manually sync the OrderProducts. Here's how you'd typically handle this in an update method:

```csharp
public async Task<OrderDto> UpdateAsync(Guid id, OrderDto orderDto, CancellationToken cancellationToken)
{
// 1. Get the tracked entity with its OrderProducts
    var existingOrder = await _repository.GetByIdAsync(
        id,
        asNoTracking: false,
        cancellationToken,
        include: q => q.Include(o => o.OrderProducts));

    if (existingOrder == null)
        throw new NotFoundException(nameof(Order), id);
    
    // 2. Validate locks
    _lockService.ValidateLockForUpdateWithChildren(existingOrder, _currentUserService.UserId);
    
    // 3. Map the simple properties (this ignores OrderProducts)
    _mapper.Map(orderDto, existingOrder);
    
    // 4. Manually sync the OrderProducts collection
    SyncOrderProducts(existingOrder, orderDto.OrderProducts);
    
    // 5. Update and save
    _repository.Update(existingOrder);
    await _unitOfWork.SaveChangesAsync(cancellationToken);
    
    return _mapper.Map<OrderDto>(existingOrder);
}

private void SyncOrderProducts(Entities.Order order, IEnumerable<OrderProductDto> orderProductDtos)
{
var dtoList = orderProductDtos.ToList();

    // Remove OrderProducts that are no longer in the DTO
    var orderProductsToRemove = order.OrderProducts
        .Where(existing => !dtoList.Any(dto => dto.Id == existing.Id))
        .ToList();
    
    foreach (var orderProduct in orderProductsToRemove)
    {
        order.OrderProducts.Remove(orderProduct);
    }
    
    // Update existing and add new OrderProducts
    foreach (var dto in dtoList)
    {
        var existing = order.OrderProducts.FirstOrDefault(op => op.Id == dto.Id);
        
        if (existing != null)
        {
            // Update existing - preserves Auditing and Locking behaviors
            existing.ProductId = dto.ProductId;
            existing.Quantity = dto.Quantity;
            existing.Price = dto.Price;
            existing.Concurrency.RowVersion = dto.RowVersion;
        }
        else
        {
            // Add new OrderProduct
            var newOrderProduct = new Entities.OrderProduct
            {
                Id = dto.Id != Guid.Empty ? dto.Id : Guid.NewGuid(),
                OrderId = order.Id,
                ProductId = dto.ProductId,
                Quantity = dto.Quantity,
                Price = dto.Price
            };
            order.OrderProducts.Add(newOrderProduct);
        }
    }
}
```
