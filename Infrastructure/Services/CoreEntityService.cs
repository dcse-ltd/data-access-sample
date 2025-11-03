using Infrastructure.Entity.Interfaces;
using Infrastructure.Repository.Interfaces;
using Infrastructure.Repository.Models;
using Infrastructure.Services.Interfaces;
using Infrastructure.Services.Models;
using Infrastructure.UnitOfWork.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Core service providing comprehensive CRUD operations for entities with support for locking, auditing, concurrency, and soft deletion.
/// Orchestrates multiple specialized services to provide a complete entity management solution.
/// </summary>
/// <typeparam name="TEntity">The entity type that implements <see cref="IEntity"/>.</typeparam>
/// <remarks>
/// This service provides a unified interface for entity operations, combining:
/// <list type="bullet">
/// <item><description><see cref="IRepository{TEntity}"/> - Data access operations</description></item>
/// <item><description><see cref="IEntityLockService{TEntity}"/> - Entity locking for concurrent access control</description></item>
/// <item><description><see cref="IEntityAuditService{TEntity}"/> - Audit trail tracking (created/modified timestamps and users)</description></item>
/// <item><description><see cref="IConcurrencyService{TEntity}"/> - Concurrency conflict handling</description></item>
/// <item><description><see cref="IEntitySoftDeleteService{TEntity}"/> - Soft delete functionality</description></item>
/// </list>
/// 
/// <para>
/// The service automatically handles:
/// <list type="bullet">
/// <item><description>Lock validation before updates/deletes</description></item>
/// <item><description>Lock unlocking after operations</description></item>
/// <item><description>Audit stamping for create/update operations</description></item>
/// <item><description>Concurrency exception handling with row version tracking</description></item>
/// <item><description>Soft delete for entities that support it</description></item>
/// </list>
/// </para>
/// 
/// <para>
/// Usage example:
/// <code>
/// // Get by ID
/// var entity = await coreEntityService.GetByIdAsync(id);
/// 
/// // Get with lock (locks entity and optionally children)
/// var lockedEntity = await coreEntityService.GetByIdWithLockAsync(id, lockOptions);
/// 
/// // Create
/// var created = await coreEntityService.CreateAsync(newEntity);
/// 
/// // Update with lock validation and unlocking
/// var updated = await coreEntityService.UpdateAsync(
///     id, 
///     entity => { /* modify entity */ },
///     lockOptions);
/// 
/// // Delete (soft delete if supported, otherwise hard delete)
/// await coreEntityService.DeleteAsync(id, lockOptions);
/// </code>
/// </para>
/// 
/// <para>
/// For operations involving child entities (e.g., Order with OrderProducts), use <see cref="ISpecification{TEntity}"/>
/// to include related entities when retrieving:
/// <code>
/// var spec = new OrderWithProductsSpecification();
/// var updated = await coreEntityService.UpdateAsync(
///     id,
///     order => { /* sync OrderProducts */ },
///     lockOptions,
///     spec); // Include OrderProducts in retrieval
/// </code>
/// </para>
/// </remarks>
public class CoreEntityService<TEntity>(
    IRepository<TEntity> repository,
    IUnitOfWork unitOfWork,
    IEntityLockService<TEntity> entityLockService,
    IEntityAuditService<TEntity> entityAuditService,
    IConcurrencyService<TEntity> concurrencyService,
    IEntitySoftDeleteService<TEntity> entitySoftDeleteService,
    ICurrentUserService currentUserService,
    ILogger<CoreEntityService<TEntity>> logger
    ) : ICoreEntityService<TEntity>
    where TEntity : class, IEntity
{
    public async Task<TEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await repository.GetByIdAsync(id, QueryOptions.Default, cancellationToken);
    }

    public async Task<TEntity?> GetByIdAsync(
        Guid id,
        QueryOptions? queryOptions,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await GetByIdAsync(id, queryOptions, null, cancellationToken);
    }
    
    public async Task<TEntity?> GetByIdAsync(
        Guid id,
        QueryOptions? queryOptions,
        ISpecification<TEntity>? specification,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        queryOptions ??= QueryOptions.Default;
        
        var hasIncludes = specification != null && (specification.Includes.Any() || specification.IncludeStrings.Any());
        logger.LogDebug(
            "Retrieving {Entity} with the Id: {EntityId} (TrackChanges: {TrackChanges}, IncludeSoftDeleted: {IncludeSoftDeleted}, HasIncludes: {HasIncludes})", 
            typeof(TEntity).Name, id, queryOptions.TrackChanges, queryOptions.IncludeSoftDeleted, hasIncludes);
        
        return await repository.GetByIdAsync(id, queryOptions, specification, cancellationToken);
    }
    
    public async Task<TEntity> GetByIdOrThrowAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await repository.GetByIdOrThrowAsync(id, QueryOptions.Default, cancellationToken);
    }

    public async Task<TEntity> GetByIdOrThrowAsync(
        Guid id,
        QueryOptions? queryOptions,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await GetByIdOrThrowAsync(id, queryOptions, null, cancellationToken);
    }
    
    public async Task<TEntity> GetByIdOrThrowAsync(
        Guid id,
        QueryOptions? queryOptions,
        ISpecification<TEntity>? specification,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        queryOptions ??= QueryOptions.Default;
        
        var hasIncludes = specification != null && (specification.Includes.Any() || specification.IncludeStrings.Any());
        logger.LogDebug(
            "Retrieving {Entity} with the Id: {EntityId} (TrackChanges: {TrackChanges}, IncludeSoftDeleted: {IncludeSoftDeleted}, HasIncludes: {HasIncludes})", 
            typeof(TEntity).Name, id, queryOptions.TrackChanges, queryOptions.IncludeSoftDeleted, hasIncludes);
        
        return await repository.GetByIdOrThrowAsync(id, queryOptions, specification, cancellationToken);
    }
    
    public async Task<TEntity> GetByIdWithLockAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await GetByIdWithLockAsync(id, null, cancellationToken);
    }

    public async Task<TEntity> GetByIdWithLockAsync(
        Guid id, 
        LockOptions? lockOptions, 
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var includeChildren = lockOptions?.IncludeChildren ?? false;
        var lockType = includeChildren ? "with cascading lock" : "with lock";
        
        logger.LogDebug("Retrieving {Entity} with the Id: {EntityId} with lock ({LockType})", typeof(TEntity).Name, id, lockType);
        var entity = await repository.GetByIdOrThrowAsync(id, QueryOptions.Tracking, cancellationToken);
        
        var userId = currentUserService.UserId;
        
        if (lockOptions?.IncludeChildren == true)
        {
            entityLockService.LockWithChildrenIfSupported(entity, userId, lockOptions.MaxDepth);
        }
        else
        {
            entityLockService.LockIfSupported(entity, userId);
        }
        
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("{Entity} with the ID: {EntityId} successfully locked to the User with the Id: {UserId}", typeof(TEntity).Name, id, userId);
        return entity;
    }

    public async Task RefreshLockAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var userId = currentUserService.UserId;
    
        logger.LogDebug("Refreshing lock for {Entity} with the Id: {EntityId} for User with the Id: {UserId}", typeof(TEntity).Name, id, userId);
    
        var entity = await repository.GetByIdOrThrowAsync(id, QueryOptions.Tracking, cancellationToken);
    
        entityLockService.RefreshLockIfOwned(entity, userId);
    
        await unitOfWork.SaveChangesAsync(cancellationToken);
    
        logger.LogInformation("{Entity} with the Id: {EntityId} lock successfully refreshed for User with the Id: {UserId}", typeof(TEntity).Name, id, userId);
    }
    
    public async Task<IEnumerable<TEntity>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await repository.GetAllAsync(cancellationToken);
    }
    
    public async Task<IEnumerable<TEntity>> GetAllAsync(
        QueryOptions? queryOptions,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        queryOptions ??= QueryOptions.Default;
        
        logger.LogDebug("Retrieving all {Entity} records (TrackChanges: {TrackChanges}, IncludeSoftDeleted: {IncludeSoftDeleted})", 
            typeof(TEntity).Name, queryOptions.TrackChanges, queryOptions.IncludeSoftDeleted);
        
        return await repository.GetAllAsync(queryOptions, cancellationToken);
    }

    public async Task<IEnumerable<TEntity>> FindAsync(
        ISpecification<TEntity> specification,
        QueryOptions? queryOptions,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(specification);
        queryOptions ??= QueryOptions.Default;

        return await repository.FindAsync(specification, queryOptions, cancellationToken);
    }

    public async Task<TEntity> CreateAsync(
        TEntity entity, 
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(entity);
        
        entity.Id = Guid.NewGuid();
        
        logger.LogDebug("Creating new {Entity} with Id: {EntityId}", typeof(TEntity).Name, entity.Id);
        await repository.AddAsync(entity, cancellationToken);
        entityAuditService.StampForCreate(entity, currentUserService.UserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        logger.LogInformation("{Entity} with Id: {EntityId} successfully created", typeof(TEntity).Name, entity.Id);
        return entity;
    }
    
    public async Task<TEntity> UpdateAsync(
        Guid id, 
        Action<TEntity> updateAction, 
        CancellationToken cancellationToken = default)
        => await UpdateAsync(id, updateAction, null, cancellationToken);

    public async Task<TEntity> UpdateAsync(
        Guid id, 
        Action<TEntity> updateAction, 
        LockOptions? lockOptions,
        CancellationToken cancellationToken = default)
        => await UpdateAsync(id, updateAction, lockOptions, null, cancellationToken);
    
    public async Task<TEntity> UpdateAsync(
        Guid id, 
        Action<TEntity> updateAction, 
        LockOptions? lockOptions,
        ISpecification<TEntity>? specification,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(updateAction);
        
        var userId = currentUserService.UserId;
        TEntity? entity = null;
        var includeChildren = lockOptions?.IncludeChildren ?? false;
        
        try
        {
            var updateType = includeChildren ? "with children" : "standard";
            logger.LogDebug("Updating {Entity} with the Id: {EntityId} in the database ({UpdateType})", typeof(TEntity).Name, id, updateType);
            
            entity = await repository.GetByIdOrThrowAsync(id, QueryOptions.Tracking, specification, cancellationToken);
            
            if (lockOptions?.IncludeChildren == true)
            {
                entityLockService.ValidateLockForUpdateWithChildren(entity, userId, lockOptions.MaxDepth);
            }
            else
            {
                entityLockService.ValidateLockForUpdate(entity, userId);
            }
            
            updateAction(entity);
        
            entityAuditService.StampForUpdate(entity, userId);
            
            // Unlock before save (change will be included in the same save since entity is tracked)
            if (lockOptions?.IncludeChildren == true)
            {
                entityLockService.UnlockWithChildrenIfSupported(entity, userId, lockOptions.MaxDepth);
            }
            else
            {
                entityLockService.UnlockIfSupported(entity, userId);
            }
            
            // Note: repository.Update() is redundant here since entity is already tracked
            await unitOfWork.SaveChangesAsync(cancellationToken);
        
            logger.LogInformation("{Entity} with the Id: {EntityId} successfully updated and unlocked", typeof(TEntity).Name, id);
            return entity;
        }
        catch (DbUpdateConcurrencyException e)
        {   
            logger.LogWarning("Concurrency conflict updating {Entity} with the Id: {EntityId}", typeof(TEntity).Name, id);

            if (entity is not null)
            {
                throw concurrencyService.HandleConcurrencyException(entity, e);
            }
            
            throw;
        }
    }

    public async Task DeleteAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await DeleteAsync(id, null, cancellationToken);
    }

    public async Task DeleteAsync(
        Guid id,
        LockOptions? lockOptions,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var userId = currentUserService.UserId;
        TEntity? entity = null;
        var includeChildren = lockOptions?.IncludeChildren ?? false;
        
        try
        {
            var deleteType = includeChildren ? "with children" : "standard";
            logger.LogDebug("Removing {Entity} with the Id: {EntityId} from the database ({DeleteType})", typeof(TEntity).Name, id, deleteType); 
            entity = await repository.GetByIdOrThrowAsync(id, QueryOptions.Tracking, cancellationToken);
        
            if (lockOptions?.IncludeChildren == true)
            {
                entityLockService.ValidateLockForUpdateWithChildren(entity, userId, lockOptions.MaxDepth);
            }
            else
            {
                entityLockService.ValidateLockForUpdate(entity, userId);
            }

            if (entity is ISoftDeletableEntity)
            {
                if (lockOptions?.IncludeChildren == true)
                {
                    entitySoftDeleteService.StampForDeleteWithChildren(entity, userId, lockOptions.MaxDepth);
                }
                else
                {
                    entitySoftDeleteService.StampForDelete(entity, userId);
                }
            }
            else
            {
                repository.Remove(entity);
            }
            
            await unitOfWork.SaveChangesAsync(cancellationToken);
        
            logger.LogInformation("{Entity} with the Id: {EntityId} successfully removed", typeof(TEntity).Name, id);
        }
        catch (DbUpdateConcurrencyException e)
        {
            logger.LogWarning("Concurrency conflict deleting {Entity} with the Id: {EntityId}", typeof(TEntity).Name, id);
            
            if (entity is not null)
            {
                throw concurrencyService.HandleConcurrencyException(entity, e);
            }
            
            throw;
        }
    }

    public async Task HardDeleteAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await HardDeleteAsync(id, null, cancellationToken);
    }

    public async Task HardDeleteAsync(
        Guid id, 
        LockOptions? lockOptions, 
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var userId = currentUserService.UserId;
        TEntity? entity = null;
        var includeChildren = lockOptions?.IncludeChildren ?? false;
        
        try
        {
            var deleteType = includeChildren ? "with children" : "standard";
            logger.LogDebug("Hard deleting {Entity} with the Id: {EntityId} from the database ({DeleteType})", typeof(TEntity).Name, id, deleteType); 
            entity = await repository.GetByIdOrThrowAsync(id, QueryOptions.Tracking, cancellationToken);
        
            if (lockOptions?.IncludeChildren == true)
            {
                entityLockService.ValidateLockForUpdateWithChildren(entity, userId, lockOptions.MaxDepth);
            }
            else
            {
                entityLockService.ValidateLockForUpdate(entity, userId);
            }

            repository.Remove(entity);
            
            await unitOfWork.SaveChangesAsync(cancellationToken);
        
            logger.LogInformation("{Entity} with the Id: {EntityId} successfully hard deleted", typeof(TEntity).Name, id);
        }
        catch (DbUpdateConcurrencyException e)
        {
            logger.LogWarning("Concurrency conflict hard deleting {Entity} with the Id: {EntityId}", typeof(TEntity).Name, id);
            
            if (entity is not null)
            {
                throw concurrencyService.HandleConcurrencyException(entity, e);
            }
            
            throw;
        }
    }

    public async Task RestoreAsync(
        Guid id, 
        bool includeChildren, 
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var userId = currentUserService.UserId;
        TEntity? entity = null;
    
        try
        {
            var restoreType = includeChildren ? "with children" : "standard";
            logger.LogDebug("Restoring soft-deleted {Entity} with the Id: {EntityId} ({RestoreType})", 
                typeof(TEntity).Name, id, restoreType);
        
            entity = await repository.GetByIdOrThrowAsync(id, QueryOptions.ForRestore, cancellationToken);
    
            if (entity is not ISoftDeletableEntity)
            {
                throw new InvalidOperationException(
                    $"{typeof(TEntity).Name} does not support soft delete and cannot be restored");
            }
    
            if (includeChildren)
            {
                entitySoftDeleteService.StampForRestoreWithChildren(entity, maxDepth: 1);
            }
            else
            {
                entitySoftDeleteService.StampForRestore(entity);
            }
        
            entityAuditService.StampForUpdate(entity, userId);
    
            // Note: repository.Update() is redundant here since entity is already tracked
            await unitOfWork.SaveChangesAsync(cancellationToken);
    
            logger.LogInformation("{Entity} with the Id: {EntityId} successfully restored", 
                typeof(TEntity).Name, id);
        }
        catch (DbUpdateConcurrencyException e)
        {
            logger.LogWarning("Concurrency conflict restoring {Entity} with the Id: {EntityId}", typeof(TEntity).Name, id);
        
            if (entity is not null)
            {
                throw concurrencyService.HandleConcurrencyException(entity, e);
            }
        
            throw;
        }
    }

}