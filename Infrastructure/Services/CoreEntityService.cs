using Infrastructure.Entity.Interfaces;
using Infrastructure.Repository.Interfaces;
using Infrastructure.Repository.Models;
using Infrastructure.Services.Interfaces;
using Infrastructure.Services.Models;
using Infrastructure.UnitOfWork.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

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
        => await repository.GetByIdAsync(id, QueryOptions.Default, cancellationToken);

    public async Task<TEntity?> GetByIdAsync(
        Guid id,
        QueryOptions? queryOptions,
        CancellationToken cancellationToken = default)
    {
        queryOptions ??= QueryOptions.Default;
        
        logger.LogInformation("Retrieving {Entity} with the Id: {EntityId} (TrackChanges: {TrackChanges}, IncludeSoftDeleted: {IncludeSoftDeleted})", 
            typeof(TEntity).Name, id, queryOptions.TrackChanges, queryOptions.IncludeSoftDeleted);
        
        return await repository.GetByIdAsync(id, queryOptions, cancellationToken);
    }
    
    public async Task<TEntity> GetByIdOrThrowAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => await repository.GetByIdOrThrowAsync(id, QueryOptions.Default, cancellationToken);

    public async Task<TEntity> GetByIdOrThrowAsync(
        Guid id,
        QueryOptions? queryOptions,
        CancellationToken cancellationToken = default)
    {
        queryOptions ??= QueryOptions.Default;
        
        logger.LogInformation("Retrieving {Entity} with the Id: {EntityId} (TrackChanges: {TrackChanges}, IncludeSoftDeleted: {IncludeSoftDeleted})", 
            typeof(TEntity).Name, id, queryOptions.TrackChanges, queryOptions.IncludeSoftDeleted);
        
        return await repository.GetByIdOrThrowAsync(id, queryOptions, cancellationToken);
    }
    
    public async Task<TEntity> GetByIdWithLockAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
        => await GetByIdWithLockAsync(id, null, cancellationToken);

    public async Task<TEntity> GetByIdWithLockAsync(
        Guid id, 
        LockOptions? lockOptions, 
        CancellationToken cancellationToken = default)
    {
        var includeChildren = lockOptions?.IncludeChildren ?? false;
        var lockType = includeChildren ? "with cascading lock" : "with lock";
        
        logger.LogInformation("Retrieving {Entity} with the Id: {EntityId} with lock ({LockType})", typeof(TEntity).Name, id, lockType);
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
        var userId = currentUserService.UserId;
    
        logger.LogInformation("Refreshing lock for {Entity} with the Id: {EntityId} for User with the Id: {UserId}", typeof(TEntity).Name, id, userId);
    
        var entity = await repository.GetByIdOrThrowAsync(id, QueryOptions.Tracking, cancellationToken);
    
        entityLockService.RefreshLockIfOwned(entity, userId);
    
        await unitOfWork.SaveChangesAsync(cancellationToken);
    
        logger.LogInformation("{Entity} with the Id: {EntityId} lock successfully refreshed for User with the Id: {UserId}", typeof(TEntity).Name, id, userId);
    }
    
    public async Task<IEnumerable<TEntity>> GetAllAsync(
        CancellationToken cancellationToken = default)
        => await repository.GetAllAsync(cancellationToken);
    
    public async Task<IEnumerable<TEntity>> GetAllAsync(
        QueryOptions? queryOptions,
        CancellationToken cancellationToken = default)
    {
        queryOptions ??= QueryOptions.Default;
        
        logger.LogInformation("Retrieving all {Entity} records (TrackChanges: {TrackChanges}, IncludeSoftDeleted: {IncludeSoftDeleted})", 
            typeof(TEntity).Name, queryOptions.TrackChanges, queryOptions.IncludeSoftDeleted);
        
        return await repository.GetAllAsync(queryOptions, cancellationToken);
    }

    public async Task<TEntity> CreateAsync(
        TEntity entity, 
        CancellationToken cancellationToken = default)
    {
        entity.Id = Guid.NewGuid();
        
        logger.LogInformation("Creating new {Entity} with Id: {EntityId}", typeof(TEntity).Name, entity.Id);
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
    {
        var userId = currentUserService.UserId;
        TEntity? entity = null;
        var includeChildren = lockOptions?.IncludeChildren ?? false;
        
        try
        {
            var updateType = includeChildren ? "with children" : "standard";
            logger.LogInformation("Updating {Entity} with the Id: {EntityId} in the database ({UpdateType})", typeof(TEntity).Name, id, updateType);
            
            entity = await repository.GetByIdOrThrowAsync(id, QueryOptions.Tracking, cancellationToken);
            
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
            
            if (lockOptions?.IncludeChildren == true)
            {
                entityLockService.UnlockWithChildrenIfSupported(entity, userId, lockOptions.MaxDepth);
            }
            else
            {
                entityLockService.UnlockIfSupported(entity, userId);
            }
            
            repository.Update(entity);
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
        => await DeleteAsync(id, null, cancellationToken);

    public async Task DeleteAsync(
        Guid id,
        LockOptions? lockOptions,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.UserId;
        TEntity? entity = null;
        var includeChildren = lockOptions?.IncludeChildren ?? false;
        
        try
        {
            var deleteType = includeChildren ? "with children" : "standard";
            logger.LogInformation("Removing {Entity} with the Id: {EntityId} from the database ({DeleteType})", typeof(TEntity).Name, id, deleteType); 
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
        => await HardDeleteAsync(id, null, cancellationToken);

    public async Task HardDeleteAsync(
        Guid id, 
        LockOptions? lockOptions, 
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.UserId;
        TEntity? entity = null;
        var includeChildren = lockOptions?.IncludeChildren ?? false;
        
        try
        {
            var deleteType = includeChildren ? "with children" : "standard";
            logger.LogInformation("Hard deleting {Entity} with the Id: {EntityId} from the database ({DeleteType})", typeof(TEntity).Name, id, deleteType); 
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
        var userId = currentUserService.UserId;
        TEntity? entity = null;
    
        try
        {
            var restoreType = includeChildren ? "with children" : "standard";
            logger.LogInformation("Restoring soft-deleted {Entity} with the Id: {EntityId} ({RestoreType})", 
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
    
            repository.Update(entity);
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