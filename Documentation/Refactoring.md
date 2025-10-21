## 1. Base Service Pattern

I'd recommend using **composition over inheritance** for the base service logic. This provides more flexibility and avoids the rigidity of abstract base classes. Here's the approach:

### Create a Core Service Component

```csharp
public interface ICoreEntityService<TEntity> where TEntity : class, IEntity
{
    Task<TEntity> GetByIdWithLockAsync(Guid id, bool lockForEdit = false, CancellationToken cancellationToken = default);
    Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<TEntity> UpdateAsync(Guid id, Action<TEntity> updateAction, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public class CoreEntityService<TEntity>(
    IRepository<TEntity> repository,
    IUnitOfWork unitOfWork,
    IEntityLockService<TEntity> lockService,
    ICurrentUserService currentUserService) : ICoreEntityService<TEntity>
    where TEntity : class, IEntity
{
    public async Task<TEntity> GetByIdWithLockAsync(
        Guid id, 
        bool lockForEdit = false, 
        CancellationToken cancellationToken = default)
    {
        var entity = await repository.GetByIdAsync(id, lockForEdit, cancellationToken);
        
        if (lockForEdit && entity is ILockableEntity<TEntity> lockableEntity)
        {
            var userId = currentUserService.UserId;
            await lockService.AcquireLockAsync(entity, userId, cancellationToken);
        }
        
        return entity;
    }

    public async Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        entity.Id = Guid.NewGuid();
        await repository.AddAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<TEntity> UpdateAsync(
        Guid id, 
        Action<TEntity> updateAction, 
        CancellationToken cancellationToken = default)
    {
        var entity = await repository.GetByIdAsync(id, trackChanges: true, cancellationToken);
        
        // Lock is validated in repository.UpdateAsync
        updateAction(entity);
        
        repository.UpdateAsync(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        return entity;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await repository.GetByIdAsync(id, trackChanges: true, cancellationToken);
        repository.Remove(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
```

Your services would then compose this:

```csharp
public class CustomerService(
    ICoreEntityService<Entities.Customer> coreService,
    IMapper mapper) : ICustomerService
{
    public async Task<CustomerDto> GetByIdAsync(
        Guid id, 
        bool lockForEdit = false, 
        CancellationToken cancellationToken = default)
    {
        var customer = await coreService.GetByIdWithLockAsync(id, lockForEdit, cancellationToken);
        return mapper.Map<CustomerDto>(customer);
    }

    public async Task<CustomerDto> UpdateAsync(CustomerDto dto, CancellationToken cancellationToken)
    {
        var updated = await coreService.UpdateAsync(
            dto.Id,
            customer => mapper.Map(dto, customer),
            cancellationToken);
        
        return mapper.Map<CustomerDto>(updated);
    }
}
```

## 2. Cascading Child Entity Locks

Implement a lock options pattern with graph traversal:

```csharp
public class LockOptions
{
    public bool IncludeChildren { get; set; }
    public List<string>? NavigationPaths { get; set; } // e.g., ["OrderProducts", "OrderProducts.Product"]
    public int MaxDepth { get; set; } = 1;
}

public interface IEntityLockService<TEntity> where TEntity : class, IEntity
{
    Task AcquireLockAsync(TEntity entity, Guid userId, CancellationToken cancellationToken = default);
    Task AcquireLockWithChildrenAsync(TEntity entity, Guid userId, LockOptions options, CancellationToken cancellationToken = default);
    Task ReleaseLockAsync(TEntity entity, Guid userId, CancellationToken cancellationToken = default);
    Task ReleaseLockWithChildrenAsync(TEntity entity, Guid userId, LockOptions options, CancellationToken cancellationToken = default);
    void ValidateLockForUpdate(TEntity entity, Guid userId);
}
```

Implementation would use EF Core's metadata model to discover relationships:

```csharp
public async Task AcquireLockWithChildrenAsync(
    TEntity entity, 
    Guid userId, 
    LockOptions options, 
    CancellationToken cancellationToken = default)
{
    await AcquireLockAsync(entity, userId, cancellationToken);
    
    if (!options.IncludeChildren) return;
    
    var navigationPaths = options.NavigationPaths ?? 
        DiscoverNavigationPaths(typeof(TEntity), options.MaxDepth);
    
    foreach (var path in navigationPaths)
    {
        var childEntities = GetChildEntitiesForPath(entity, path);
        foreach (var child in childEntities)
        {
            if (child is ILockableEntity lockableChild)
            {
                await AcquireLockAsync((TEntity)(object)child, userId, cancellationToken);
            }
        }
    }
}
```

## 3. Specification Pattern

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

## Summary

This approach gives you:

1. **Reusable core logic** via composition without tight coupling
2. **Flexible child locking** with configurable depth and paths
3. **Powerful querying** with specifications that separate query logic from repositories

All three features integrate naturally with your existing architecture and EF Core 9. Would you like me to elaborate on any specific aspect or provide more implementation details?

## Separation of Concerns

The repository should focus purely on **data access operations** (CRUD against the database), while the service layer handles **business logic and orchestration** (which includes locking semantics).

### Current Problem

Your repository is currently mixing concerns:

```csharp
public TEntity UpdateAsync(TEntity entity)
{   
    var userId = currentUserService.UserId;
    entityLockService.ValidateLockForUpdate(entity, userId);  // ← Business logic
    _dbSet.Update(entity);  // ← Data access
    return entity;
}
```

This means:
- The repository depends on services it shouldn't know about (`IEntityLockService`, `ICurrentUserService`)
- Lock validation logic is duplicated across `UpdateAsync` and `Remove`
- You can't use the repository without the locking mechanism
- Testing becomes more complex

## Recommended Refactoring

### 1. Simplify the Repository

Remove lock validation - make it a pure data access layer:

<llm-snippet-file>DataAccess/Repository/Repository.cs</llm-snippet-file>
```csharp
using DataAccess.Context;
using DataAccess.Entity.Interfaces;
using DataAccess.Exceptions;
using DataAccess.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repository;

public class Repository<TEntity>(BaseAppDbContext context) : IRepository<TEntity>
    where TEntity : class, IEntity
{
    private readonly DbSet<TEntity> _dbSet = context.Set<TEntity>();

    public async Task<TEntity> GetByIdAsync(
        Guid id, 
        bool trackChanges = true,
        CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? _dbSet : _dbSet.AsNoTracking();
        var entity = await query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        
        return entity ?? throw new EntityNotFoundException(typeof(TEntity).Name, id);
    }
    
    public async Task<IEnumerable<TEntity>> GetAllAsync(
        CancellationToken cancellationToken) 
        => await _dbSet.AsNoTracking().ToListAsync(cancellationToken);
    
    public async Task AddAsync(
        TEntity entity, 
        CancellationToken cancellationToken) 
        => await _dbSet.AddAsync(entity, cancellationToken);

    public TEntity Update(TEntity entity)
    {   
        _dbSet.Update(entity);
        return entity;
    }

    public void Remove(TEntity entity)
    {
        _dbSet.Remove(entity);   
    }
}
```

### 2. Move Lock Logic to Core Service

The `CoreEntityService` now handles all locking concerns:

```csharp
public class CoreEntityService<TEntity>(
    IRepository<TEntity> repository,
    IUnitOfWork unitOfWork,
    IEntityLockService<TEntity> lockService,
    ICurrentUserService currentUserService) : ICoreEntityService<TEntity>
    where TEntity : class, IEntity
{
    public async Task<TEntity> GetByIdWithLockAsync(
        Guid id, 
        bool lockForEdit = false, 
        CancellationToken cancellationToken = default)
    {
        var entity = await repository.GetByIdAsync(id, lockForEdit, cancellationToken);
        
        if (lockForEdit && entity is ILockableEntity<TEntity> lockableEntity)
        {
            var userId = currentUserService.UserId;
            await lockService.AcquireLockAsync(entity, userId, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken); // Persist the lock
        }
        
        return entity;
    }

    public async Task<TEntity> UpdateAsync(
        Guid id, 
        Action<TEntity> updateAction, 
        CancellationToken cancellationToken = default)
    {
        var entity = await repository.GetByIdAsync(id, trackChanges: true, cancellationToken);
        
        // Validate lock before update
        if (entity is ILockableEntity<TEntity>)
        {
            var userId = currentUserService.UserId;
            lockService.ValidateLockForUpdate(entity, userId);
        }
        
        updateAction(entity);
        
        repository.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        // Release lock after successful update
        if (entity is ILockableEntity<TEntity>)
        {
            var userId = currentUserService.UserId;
            await lockService.ReleaseLockAsync(entity, userId, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        
        return entity;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await repository.GetByIdAsync(id, trackChanges: true, cancellationToken);
        
        // Validate lock before delete
        if (entity is ILockableEntity<TEntity>)
        {
            var userId = currentUserService.UserId;
            lockService.ValidateLockForUpdate(entity, userId);
        }
        
        repository.Remove(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
```

### 3. Update Your Service

Your `CustomerService` becomes even simpler:

<llm-snippet-file>Features/Customer/Services/CustomerService.cs</llm-snippet-file>
```csharp
using AutoMapper;
using DataAccess.Exceptions;
using Features.Customer.Dtos;
using Features.Customer.Interfaces;
using Microsoft.Extensions.Logging;

namespace Features.Customer.Services;

public class CustomerService(
    ICoreEntityService<Entities.Customer> coreService,
    IMapper mapper,
    ILogger<CustomerService> logger) : ICustomerService
{
    public async Task<IEnumerable<CustomerDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var customers = await coreService.GetAllAsync(cancellationToken);
        return customers.Select(mapper.Map<CustomerDto>);
    }

    public async Task<CustomerDto> GetByIdAsync(
        Guid id, 
        bool lockForEdit = false, 
        CancellationToken cancellationToken = default)
    {
        var customer = await coreService.GetByIdWithLockAsync(id, lockForEdit, cancellationToken);
        return mapper.Map<CustomerDto>(customer);
    }

    public async Task<CustomerDto> CreateAsync(CustomerDto customer, CancellationToken cancellationToken)
    {
        var newCustomer = mapper.Map<Entities.Customer>(customer);
        var created = await coreService.CreateAsync(newCustomer, cancellationToken);
        return mapper.Map<CustomerDto>(created);
    }

    public async Task<CustomerDto> UpdateAsync(CustomerDto customer, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await coreService.UpdateAsync(
                customer.Id,
                existingCustomer => mapper.Map(customer, existingCustomer),
                cancellationToken);

            return mapper.Map<CustomerDto>(updated);
        }
        catch (ConcurrencyException e)
        {
            logger.LogWarning(
                "Concurrency conflict updating customer {CustomerId}: {Message}",
                customer.Id,
                e.Message);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await coreService.DeleteAsync(id, cancellationToken);
    }
}
```

## Benefits of This Approach

1. **Clean Separation**: Repository = data access only, Service = business logic
2. **Flexibility**: You can use the repository without locks if needed
3. **Testability**: Repository tests don't need to mock lock services
4. **Consistency**: All lock logic is centralized in one place
5. **Reusability**: The repository can be used in contexts where locking isn't needed
6. **Single Responsibility**: Each layer has one clear purpose

## Optional: Make Lock Behavior Configurable

If you want even more flexibility in your `CoreEntityService`:

```csharp
public class EntityOperationOptions
{
    public bool AutoLock { get; set; } = true;
    public bool AutoReleaseLock { get; set; } = true;
    public LockOptions? ChildLockOptions { get; set; }
}

public async Task<TEntity> UpdateAsync(
    Guid id, 
    Action<TEntity> updateAction,
    EntityOperationOptions? options = null,
    CancellationToken cancellationToken = default)
{
    options ??= new EntityOperationOptions();
    
    // Lock validation only if AutoLock is true
    // ... rest of implementation
}
```

This gives services full control over when and how locking is applied, while keeping the repository completely agnostic to these concerns.

