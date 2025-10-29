using System.Collections;
using System.Reflection;
using Infrastructure.Entity.Attributes;
using Infrastructure.Entity.Interfaces;
using Infrastructure.Entity.Models;
using Infrastructure.Exceptions;
using Infrastructure.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class EntityLockService<TEntity>(
    ILogger<EntityLockService<TEntity>> logger
    ) : IEntityLockService<TEntity>
    where TEntity : class, IEntity
{
    public void LockIfSupported(TEntity entity, Guid userId)
    {
        if (entity is ILockableEntity<TEntity> lockable)
        {
            logger.LogInformation("Locking {Entity} with the Id: {EntityId}, to the User with the ID {UserId}", typeof(TEntity).Name, entity.Id, userId);
            lockable.Locking.Lock(userId);
        }
        else
        {
            logger.LogInformation("{Entity} with the Id: {EntityId} is not lockable", typeof(TEntity).Name, entity.Id);
        }
    }
    
    public void LockWithChildrenIfSupported(TEntity entity, Guid userId, int maxDepth = 1)
    {
        LockIfSupported(entity, userId);
        LockChildrenRecursive(entity, userId, currentDepth: 0, maxDepth);
    }

    public bool UnlockIfSupported(TEntity entity, Guid userId)
    {
        if (entity is not ILockableEntity<TEntity> lockable) 
            return true;
        
        logger.LogInformation("Unlocking {Entity} with the Id: {EntityId}, from the User with the ID {UserId}", typeof(TEntity).Name, entity.Id, userId);
        return lockable.Locking.Unlock(userId);
    }
    
    public bool UnlockWithChildrenIfSupported(TEntity entity, Guid userId, int maxDepth = 1)
    {
        var result = UnlockIfSupported(entity, userId);
        UnlockChildrenRecursive(entity, userId, currentDepth: 0, maxDepth);
        return result;
    }

    public void ValidateLockForUpdate(TEntity entity, Guid userId)
    {
        logger.LogInformation("Validating {Entity} with the Id: {EntityId} can be unlocked by User with the Id: {UserId}", typeof(TEntity).Name, entity.Id, userId);
        
        if (entity is not ILockableEntity<TEntity> lockable)
        {
            logger.LogInformation("{Entity} with the Id: {EntityId} is not lockable", typeof(TEntity).Name, entity.Id);
            return;
        }
        
        var lockInfo = lockable.Locking.LockInfo;

        if (lockInfo.LockedByUserId == null)
        {
            logger.LogInformation("{Entity} with the Id: {EntityId} has not been locked prior to update", typeof(TEntity).Name, entity.Id);
            throw new EntityUnlockedException(typeof(TEntity).Name, entity.Id);
        }
        
        if (lockInfo.IsExpired())
        {
            logger.LogInformation("{Entity} with the Id: {EntityId} has an expired lock, forcing unlock", typeof(TEntity).Name, entity.Id);
            
            throw new EntityLockExpiredException(
                typeof(TEntity).Name,
                entity.Id,
                lockInfo.LockedByUserId.Value,
                lockInfo.LockedAtUtc!.Value,
                lockInfo.LockTimeoutMinutes);
        }

        if (lockInfo.IsLockedBy(userId))
        {
            logger.LogInformation("{Entity} with the Id: {EntityId} is locked to the User with the Id: {UserId}. Update can proceed", typeof(TEntity).Name, entity.Id, userId);
            return;
        }

        logger.LogInformation("{Entity} with the Id: {EntityId} has cannot be unlocked for the User with the Id: {UserId}", typeof(TEntity).Name, entity.Id, userId);
        throw new EntityLockedException(
            typeof(TEntity).Name,
            lockInfo.LockedByUserId.Value,
            lockInfo.LockedAtUtc);
    }
    
    public void ValidateLockForUpdateWithChildren(TEntity entity, Guid userId, int maxDepth = 1)
    {
        ValidateLockForUpdate(entity, userId);
        ValidateChildrenLocksRecursive(entity, userId, currentDepth: 0, maxDepth);
    }

    public void RefreshLockIfOwned(TEntity entity, Guid userId)
    {
        if (entity is not ILockableEntity<TEntity> lockable) 
            return;
        
        logger.LogInformation("Refreshing lock for {Entity} with the Id: {EntityId} to User with the Id: {UserId}", typeof(TEntity).Name, entity.Id, userId);
        lockable.Locking.RefreshLock(userId);
    }
    
    public void RefreshLockWithChildrenIfOwned(TEntity entity, Guid userId, int maxDepth = 1)
    {
        RefreshLockIfOwned(entity, userId);
        RefreshChildrenLocksRecursive(entity, userId, currentDepth: 0, maxDepth);
    }

    public bool IsLockedByAnotherUser(TEntity entity, Guid userId)
    {
        if (entity is not ILockableEntity<TEntity> lockable)
        {
            logger.LogInformation("{Entity} with the Id: {EntityId} is not lockable", typeof(TEntity).Name, entity.Id);
            return false;
        }
        
        var lockInfo = lockable.Locking.LockInfo;
        if (lockInfo.LockedByUserId == null || lockInfo.IsExpired())
        {
            logger.LogInformation("{Entity} with the Id: {EntityId} is currently unlocked or the lock is expired.", typeof(TEntity).Name, entity.Id);
            return false;
        }

        var isNotLockedByUserId = !lockInfo.IsLockedBy(userId);
        var logMessage = isNotLockedByUserId
            ? "{Entity} with the Id: {EntityId} is not locked to the User with the Id: {UserId}"
            : "{Entity} with the Id: {EntityId} is locked to the User with the Id: {UserId}";
        logger.LogInformation(logMessage, typeof(TEntity).Name, entity.Id, lockInfo.LockedByUserId);
        return isNotLockedByUserId;
    }
    
    public void ForceUnlockIfSupported(TEntity entity)
    {
        if (entity is not ILockableEntity<TEntity> lockable) 
            return;
        
        logger.LogInformation("{Entity} with the Id: {EntityId} will be forcefully unlocked", typeof(TEntity).Name, entity.Id);
        lockable.Locking.ForceUnlock();
    }

    public void ForceUnlockWithChildrenIfSupported(TEntity entity, int maxDepth = 1)
    {
        ForceUnlockIfSupported(entity);
        ForceUnlockChildrenRecursive(entity, currentDepth: 0, maxDepth);
    }
    
    private void LockChildrenRecursive(object entity, Guid userId, int currentDepth, int maxDepth)
    {
        if (currentDepth >= maxDepth) return;

        var entityType = entity.GetType();
        var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            if (property.GetCustomAttribute<CascadeLockAttribute>() == null)
                continue;

            var value = property.GetValue(entity);
            if (value == null)
                continue;

            if (value is IEnumerable enumerable and not string)
            {
                foreach (var child in enumerable)
                {
                    LockChild(child, userId, currentDepth, maxDepth);
                }
            }
            else
            {
                LockChild(value, userId, currentDepth, maxDepth);
            }
        }
    }

    private void LockChild(object child, Guid userId, int currentDepth, int maxDepth)
    {
        if (child is not ILockableEntity<TEntity>) 
            return;

        var childEntityType = child.GetType();
        var entityInterface = childEntityType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ILockableEntity<>));

        if (entityInterface == null) 
            return;

        var lockingProperty = childEntityType.GetProperty("Locking");
        if (lockingProperty == null) 
            return;

        var locking = lockingProperty.GetValue(child);
        if (locking == null) 
            return;

        var lockMethod = locking.GetType().GetMethod("Lock");
        if (lockMethod == null) 
            return;

        try
        {
            logger.LogInformation("Locking child {Entity} to User with the ID {UserId}", childEntityType.Name, userId);
            lockMethod.Invoke(locking, [userId]);
            LockChildrenRecursive(child, userId, currentDepth + 1, maxDepth);
        }
        catch (TargetInvocationException ex)
        {
            if (ex.InnerException != null)
                throw ex.InnerException;
            throw;
        }
    }

    private void UnlockChildrenRecursive(object entity, Guid userId, int currentDepth, int maxDepth)
    {
        if (currentDepth >= maxDepth) return;

        var entityType = entity.GetType();
        var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            if (property.GetCustomAttribute<CascadeLockAttribute>() == null)
                continue;

            var value = property.GetValue(entity);
            if (value == null)
                continue;

            if (value is IEnumerable enumerable and not string)
            {
                foreach (var child in enumerable)
                {
                    UnlockChild(child, userId, currentDepth, maxDepth);
                }
            }
            else
            {
                UnlockChild(value, userId, currentDepth, maxDepth);
            }
        }
    }

    private void UnlockChild(object child, Guid userId, int currentDepth, int maxDepth)
    {
        if (child is not ILockableEntity<TEntity>) 
            return;

        var childEntityType = child.GetType();
        var lockingProperty = childEntityType.GetProperty("Locking");
        if (lockingProperty == null) 
            return;

        var locking = lockingProperty.GetValue(child);
        if (locking == null) 
            return;

        var unlockMethod = locking.GetType().GetMethod("Unlock");
        if (unlockMethod == null) 
            return;

        logger.LogInformation("Unlocking child {Entity} from User with the ID {UserId}", childEntityType.Name, userId);
        unlockMethod.Invoke(locking, [userId]);
        UnlockChildrenRecursive(child, userId, currentDepth + 1, maxDepth);
    }

    private void ValidateChildrenLocksRecursive(object entity, Guid userId, int currentDepth, int maxDepth)
    {
        if (currentDepth >= maxDepth) return;

        var entityType = entity.GetType();
        var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            if (property.GetCustomAttribute<CascadeLockAttribute>() == null)
                continue;

            var value = property.GetValue(entity);
            if (value == null)
                continue;

            if (value is IEnumerable enumerable and not string)
            {
                foreach (var child in enumerable)
                {
                    ValidateChildLock(child, userId, currentDepth, maxDepth);
                }
            }
            else
            {
                ValidateChildLock(value, userId, currentDepth, maxDepth);
            }
        }
    }

    private void ValidateChildLock(object child, Guid userId, int currentDepth, int maxDepth)
    {
        if (child is not ILockableEntity<TEntity>) 
            return;

        var childEntityType = child.GetType();
        var lockingProperty = childEntityType.GetProperty("Locking");
        if (lockingProperty == null) 
            return;

        var locking = lockingProperty.GetValue(child);
        if (locking == null) 
            return;

        var lockInfoProperty = locking.GetType().GetProperty("LockInfo");
        if (lockInfoProperty == null) 
            return;

        var lockInfo = lockInfoProperty.GetValue(locking) as LockInfo;
        if (lockInfo == null) 
            return;

        var entityIdProperty = childEntityType.GetProperty("Id");
        var entityId = entityIdProperty?.GetValue(child) as Guid? ?? Guid.Empty;

        logger.LogInformation("Validating child {Entity} with the Id: {EntityId} can be unlocked by User with the Id: {UserId}", 
            childEntityType.Name, entityId, userId);

        if (lockInfo.LockedByUserId == null)
        {
            logger.LogInformation("Child {Entity} with the Id: {EntityId} has not been locked prior to update", 
                childEntityType.Name, entityId);
            throw new EntityUnlockedException(childEntityType.Name, entityId);
        }

        if (lockInfo.IsExpired())
        {
            logger.LogInformation("Child {Entity} with the Id: {EntityId} has an expired lock, forcing unlock", 
                childEntityType.Name, entityId);
            var forceUnlockMethod = locking.GetType().GetMethod("ForceUnlock");
            forceUnlockMethod?.Invoke(locking, null);
            return;
        }

        if (lockInfo.IsLockedBy(userId))
        {
            logger.LogInformation("Child {Entity} with the Id: {EntityId} is locked to the User with the Id: {UserId}. Update can proceed", 
                childEntityType.Name, entityId, userId);
            ValidateChildrenLocksRecursive(child, userId, currentDepth + 1, maxDepth);
            return;
        }

        logger.LogInformation("Child {Entity} with the Id: {EntityId} cannot be unlocked for the User with the Id: {UserId}", 
            childEntityType.Name, entityId, userId);
        throw new EntityLockedException(
            childEntityType.Name,
            lockInfo.LockedByUserId.Value,
            lockInfo.LockedAtUtc);
    }

    private void RefreshChildrenLocksRecursive(object entity, Guid userId, int currentDepth, int maxDepth)
    {
        if (currentDepth >= maxDepth) return;

        var entityType = entity.GetType();
        var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            if (property.GetCustomAttribute<CascadeLockAttribute>() == null)
                continue;

            var value = property.GetValue(entity);
            if (value == null)
                continue;

            if (value is IEnumerable enumerable and not string)
            {
                foreach (var child in enumerable)
                {
                    RefreshChildLock(child, userId, currentDepth, maxDepth);
                }
            }
            else
            {
                RefreshChildLock(value, userId, currentDepth, maxDepth);
            }
        }
    }

    private void RefreshChildLock(object child, Guid userId, int currentDepth, int maxDepth)
    {
        if (child is not ILockableEntity<TEntity>) 
            return;

        var childEntityType = child.GetType();
        var lockingProperty = childEntityType.GetProperty("Locking");
        if (lockingProperty == null) 
            return;

        var locking = lockingProperty.GetValue(child);
        if (locking == null) 
            return;

        var refreshLockMethod = locking.GetType().GetMethod("RefreshLock");
        if (refreshLockMethod == null) 
            return;

        var entityIdProperty = childEntityType.GetProperty("Id");
        var entityId = entityIdProperty?.GetValue(child) as Guid? ?? Guid.Empty;

        logger.LogInformation("Refreshing lock for child {Entity} with the Id: {EntityId} to User with the Id: {UserId}", 
            childEntityType.Name, entityId, userId);
        refreshLockMethod.Invoke(locking, [userId]);
        RefreshChildrenLocksRecursive(child, userId, currentDepth + 1, maxDepth);
    }

    private void ForceUnlockChildrenRecursive(object entity, int currentDepth, int maxDepth)
    {
        if (currentDepth >= maxDepth) return;

        var entityType = entity.GetType();
        var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            if (property.GetCustomAttribute<CascadeLockAttribute>() == null)
                continue;

            var value = property.GetValue(entity);
            if (value == null)
                continue;

            if (value is IEnumerable enumerable and not string)
            {
                foreach (var child in enumerable)
                {
                    ForceUnlockChild(child, currentDepth, maxDepth);
                }
            }
            else
            {
                ForceUnlockChild(value, currentDepth, maxDepth);
            }
        }
    }

    private void ForceUnlockChild(object child, int currentDepth, int maxDepth)
    {
        if (child is not ILockableEntity<TEntity>) 
            return;

        var childEntityType = child.GetType();
        var lockingProperty = childEntityType.GetProperty("Locking");
        if (lockingProperty == null) 
            return;

        var locking = lockingProperty.GetValue(child);
        if (locking == null) 
            return;

        var forceUnlockMethod = locking.GetType().GetMethod("ForceUnlock");
        if (forceUnlockMethod == null) 
            return;

        var entityIdProperty = childEntityType.GetProperty("Id");
        var entityId = entityIdProperty?.GetValue(child) as Guid? ?? Guid.Empty;

        logger.LogInformation("Forcefully unlocking child {Entity} with the Id: {EntityId}", childEntityType.Name, entityId);
        forceUnlockMethod.Invoke(locking, null);
        ForceUnlockChildrenRecursive(child, currentDepth + 1, maxDepth);
    }
}