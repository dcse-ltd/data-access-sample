using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using Infrastructure.Entity.Attributes;
using Infrastructure.Entity.Interfaces;
using Infrastructure.Entity.Models;
using Infrastructure.Exceptions;
using Infrastructure.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Service for managing entity locking functionality, including locking, unlocking, validation, and refresh operations.
/// Supports both single entity locking and cascading locks for parent-child relationships.
/// </summary>
/// <typeparam name="TEntity">The entity type that implements <see cref="IEntity"/> and optionally <see cref="ILockableEntity{TEntity}"/>.</typeparam>
/// <remarks>
/// This service provides comprehensive lock management for entities that implement <see cref="ILockableEntity{TEntity}"/>.
/// It supports:
/// <list type="bullet">
/// <item><description>Locking/unlocking entities</description></item>
/// <item><description>Cascading locks to child entities via <see cref="CascadeLockAttribute"/></description></item>
/// <item><description>Validating locks before updates</description></item>
/// <item><description>Refreshing locks to extend expiration</description></item>
/// <item><description>Force unlocking entities (administrative operation)</description></item>
/// </list>
/// 
/// Lock validation ensures that:
/// <list type="bullet">
/// <item><description>The entity is locked before modification</description></item>
/// <item><description>The lock belongs to the current user</description></item>
/// <item><description>The lock hasn't expired</description></item>
/// <item><description>Child entities are also properly locked (when using cascading validation)</description></item>
/// </list>
/// 
/// Usage example:
/// <code>
/// // Lock an entity
/// entityLockService.LockIfSupported(entity, userId);
/// 
/// // Lock with cascading to children
/// entityLockService.LockWithChildrenIfSupported(entity, userId, maxDepth: 1);
/// 
/// // Validate lock before update
/// entityLockService.ValidateLockForUpdateWithChildren(entity, userId, maxDepth: 1);
/// 
/// // Unlock after update
/// entityLockService.UnlockWithChildrenIfSupported(entity, userId, maxDepth: 1);
/// </code>
/// </remarks>
public class EntityLockService<TEntity>(
    ILogger<EntityLockService<TEntity>> logger
    ) : IEntityLockService<TEntity>
    where TEntity : class, IEntity
{
    /// <summary>
    /// Static cache for reflection metadata to improve performance when working with child entities.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, LockableEntityReflectionInfo> ReflectionCache = new();

    /// <summary>
    /// Cached reflection information for lockable entities.
    /// </summary>
    private class LockableEntityReflectionInfo
    {
        public PropertyInfo? LockingProperty { get; set; }
        public PropertyInfo? LockInfoProperty { get; set; }
        public PropertyInfo? IdProperty { get; set; }
        public MethodInfo? LockMethod { get; set; }
        public MethodInfo? UnlockMethod { get; set; }
        public MethodInfo? RefreshLockMethod { get; set; }
        public MethodInfo? ForceUnlockMethod { get; set; }
    }

    /// <summary>
    /// Checks if an entity implements ILockableEntity<> regardless of the generic type parameter.
    /// </summary>
    private static bool IsLockableEntity(object entity)
    {
        if (entity == null)
            return false;

        var entityType = entity.GetType();
        return entityType.GetInterfaces()
            .Any(i => i.IsGenericType && 
                      i.GetGenericTypeDefinition() == typeof(ILockableEntity<>));
    }

    /// <summary>
    /// Gets or creates cached reflection information for the specified entity type.
    /// </summary>
    private static LockableEntityReflectionInfo GetReflectionInfo(Type entityType)
    {
        return ReflectionCache.GetOrAdd(entityType, type =>
        {
            var info = new LockableEntityReflectionInfo
            {
                LockingProperty = type.GetProperty("Locking", BindingFlags.Public | BindingFlags.Instance),
                IdProperty = type.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance)
            };

            if (info.LockingProperty != null)
            {
                var lockingType = info.LockingProperty.PropertyType;
                info.LockInfoProperty = lockingType.GetProperty("LockInfo", BindingFlags.Public | BindingFlags.Instance);
                info.LockMethod = lockingType.GetMethod("Lock", BindingFlags.Public | BindingFlags.Instance);
                info.UnlockMethod = lockingType.GetMethod("Unlock", BindingFlags.Public | BindingFlags.Instance);
                info.RefreshLockMethod = lockingType.GetMethod("RefreshLock", BindingFlags.Public | BindingFlags.Instance);
                info.ForceUnlockMethod = lockingType.GetMethod("ForceUnlock", BindingFlags.Public | BindingFlags.Instance);
            }

            return info;
        });
    }
    /// <summary>
    /// Locks an entity for the specified user if the entity supports locking.
    /// </summary>
    /// <param name="entity">The entity to lock.</param>
    /// <param name="userId">The ID of the user locking the entity.</param>
    /// <remarks>
    /// If the entity doesn't implement <see cref="ILockableEntity{TEntity}"/>, this method does nothing.
    /// Locking prevents other users from modifying the entity until it's unlocked or the lock expires.
    /// </remarks>
    public void LockIfSupported(TEntity entity, Guid userId)
    {
        if (entity is ILockableEntity<TEntity> lockable)
        {
            logger.LogDebug("Locking {Entity} with the Id: {EntityId}, to the User with the ID {UserId}", typeof(TEntity).Name, entity.Id, userId);
            lockable.Locking.Lock(userId);
            logger.LogInformation("{Entity} with the Id: {EntityId} successfully locked to User with the ID {UserId}", typeof(TEntity).Name, entity.Id, userId);
        }
        else
        {
            logger.LogDebug("{Entity} with the Id: {EntityId} is not lockable", typeof(TEntity).Name, entity.Id);
        }
    }
    
    /// <summary>
    /// Locks an entity and its child entities recursively based on <see cref="CascadeLockAttribute"/>.
    /// </summary>
    /// <param name="entity">The entity to lock.</param>
    /// <param name="userId">The ID of the user locking the entity.</param>
    /// <param name="maxDepth">The maximum depth to traverse when locking children (default: 1).</param>
    /// <remarks>
    /// This method locks the entity first, then recursively locks all child entities 
    /// marked with <see cref="CascadeLockAttribute"/> up to the specified depth.
    /// Useful when you want to prevent modifications to a parent entity and all its related children.
    /// </remarks>
    public void LockWithChildrenIfSupported(TEntity entity, Guid userId, int maxDepth = 1)
    {
        LockIfSupported(entity, userId);
        LockChildrenRecursive(entity, userId, currentDepth: 0, maxDepth);
    }

    /// <summary>
    /// Unlocks an entity for the specified user if the entity supports locking and the user owns the lock.
    /// </summary>
    /// <param name="entity">The entity to unlock.</param>
    /// <param name="userId">The ID of the user unlocking the entity.</param>
    /// <returns>True if the entity was unlocked or doesn't support locking; false if the user doesn't own the lock.</returns>
    /// <remarks>
    /// If the entity doesn't implement <see cref="ILockableEntity{TEntity}"/>, returns true.
    /// If the user doesn't own the lock, returns false and the lock remains unchanged.
    /// </remarks>
    public bool UnlockIfSupported(TEntity entity, Guid userId)
    {
        if (entity is not ILockableEntity<TEntity> lockable) 
            return true;
        
        logger.LogDebug("Unlocking {Entity} with the Id: {EntityId}, from the User with the ID {UserId}", typeof(TEntity).Name, entity.Id, userId);
        var result = lockable.Locking.Unlock(userId);
        if (result)
        {
            logger.LogInformation("{Entity} with the Id: {EntityId} successfully unlocked from User with the ID {UserId}", typeof(TEntity).Name, entity.Id, userId);
        }
        return result;
    }
    
    /// <summary>
    /// Unlocks an entity and its child entities recursively based on <see cref="CascadeLockAttribute"/>.
    /// </summary>
    /// <param name="entity">The entity to unlock.</param>
    /// <param name="userId">The ID of the user unlocking the entity.</param>
    /// <param name="maxDepth">The maximum depth to traverse when unlocking children (default: 1).</param>
    /// <returns>True if the entity was unlocked or doesn't support locking; false if the user doesn't own the lock.</returns>
    /// <remarks>
    /// This method unlocks the entity first, then recursively unlocks all child entities 
    /// marked with <see cref="CascadeLockAttribute"/> up to the specified depth.
    /// </remarks>
    public bool UnlockWithChildrenIfSupported(TEntity entity, Guid userId, int maxDepth = 1)
    {
        var result = UnlockIfSupported(entity, userId);
        UnlockChildrenRecursive(entity, userId, currentDepth: 0, maxDepth);
        return result;
    }

    /// <summary>
    /// Validates that an entity is locked and can be updated by the specified user.
    /// Throws an exception if the entity is not locked, locked by another user, or the lock has expired.
    /// </summary>
    /// <param name="entity">The entity to validate.</param>
    /// <param name="userId">The ID of the user attempting to update.</param>
    /// <exception cref="EntityUnlockedException">
    /// Thrown if the entity is not locked before the update operation.
    /// </exception>
    /// <exception cref="EntityLockExpiredException">
    /// Thrown if the entity's lock has expired.
    /// </exception>
    /// <exception cref="EntityLockedException">
    /// Thrown if the entity is locked by a different user.
    /// </exception>
    /// <remarks>
    /// This method should be called before modifying an entity to ensure proper lock ownership.
    /// If the entity doesn't implement <see cref="ILockableEntity{TEntity}"/>, validation is skipped.
    /// 
    /// Validation checks:
    /// <list type="bullet">
    /// <item><description>The entity is locked (not null)</description></item>
    /// <item><description>The lock hasn't expired</description></item>
    /// <item><description>The lock belongs to the current user</description></item>
    /// </list>
    /// </remarks>
    public void ValidateLockForUpdate(TEntity entity, Guid userId)
    {
        logger.LogDebug("Validating {Entity} with the Id: {EntityId} can be updated by User with the Id: {UserId}", typeof(TEntity).Name, entity.Id, userId);
        
        if (entity is not ILockableEntity<TEntity> lockable)
        {
            logger.LogDebug("{Entity} with the Id: {EntityId} is not lockable", typeof(TEntity).Name, entity.Id);
            return;
        }
        
        var lockInfo = lockable.Locking.LockInfo;

        if (lockInfo.LockedByUserId == null)
        {
            logger.LogWarning("{Entity} with the Id: {EntityId} has not been locked prior to update", typeof(TEntity).Name, entity.Id);
            throw new EntityUnlockedException(typeof(TEntity).Name, entity.Id);
        }
        
        if (lockInfo.IsExpired())
        {
            var expiredAt = lockInfo.LockedAtUtc!.Value.AddMinutes(lockInfo.LockTimeoutMinutes);
            logger.LogWarning(
                "{Entity} with the Id: {EntityId} has an expired lock. Locked by User {LockedByUserId} at {LockedAtUtc:yyyy-MM-dd HH:mm:ss} UTC, expired at {ExpiredAtUtc:yyyy-MM-dd HH:mm:ss} UTC after {TimeoutMinutes} minutes. Attempted access by User {CurrentUserId}",
                typeof(TEntity).Name, entity.Id, lockInfo.LockedByUserId.Value, lockInfo.LockedAtUtc.Value, expiredAt, lockInfo.LockTimeoutMinutes, userId);
            
            throw new EntityLockExpiredException(
                typeof(TEntity).Name,
                entity.Id,
                lockInfo.LockedByUserId.Value,
                lockInfo.LockedAtUtc.Value,
                lockInfo.LockTimeoutMinutes);
        }

        if (lockInfo.IsLockedBy(userId))
        {
            logger.LogInformation("{Entity} with the Id: {EntityId} is locked to the User with the Id: {UserId}. Update can proceed", typeof(TEntity).Name, entity.Id, userId);
            return;
        }

        logger.LogWarning(
            "{Entity} with the Id: {EntityId} is locked by User {LockedByUserId} (locked at {LockedAtUtc:yyyy-MM-dd HH:mm:ss} UTC) and cannot be updated by User {CurrentUserId}",
            typeof(TEntity).Name, entity.Id, lockInfo.LockedByUserId.Value, lockInfo.LockedAtUtc?.ToString("yyyy-MM-dd HH:mm:ss") ?? "unknown", userId);
        throw new EntityLockedException(
            typeof(TEntity).Name,
            lockInfo.LockedByUserId.Value,
            lockInfo.LockedAtUtc);
    }
    
    /// <summary>
    /// Validates that an entity and its child entities are locked and can be updated by the specified user.
    /// Recursively validates locks on child entities marked with <see cref="CascadeLockAttribute"/>.
    /// </summary>
    /// <param name="entity">The entity to validate.</param>
    /// <param name="userId">The ID of the user attempting to update.</param>
    /// <param name="maxDepth">The maximum depth to traverse when validating children (default: 1).</param>
    /// <exception cref="EntityUnlockedException">
    /// Thrown if the entity or any child entity is not locked before the update operation.
    /// </exception>
    /// <exception cref="EntityLockExpiredException">
    /// Thrown if the entity's or any child entity's lock has expired.
    /// </exception>
    /// <exception cref="EntityLockedException">
    /// Thrown if the entity or any child entity is locked by a different user.
    /// </exception>
    /// <remarks>
    /// This method is typically called before updating an entity with related child entities
    /// to ensure all entities in the hierarchy are properly locked by the current user.
    /// </remarks>
    public void ValidateLockForUpdateWithChildren(TEntity entity, Guid userId, int maxDepth = 1)
    {
        ValidateLockForUpdate(entity, userId);
        ValidateChildrenLocksRecursive(entity, userId, currentDepth: 0, maxDepth);
    }

    /// <summary>
    /// Refreshes the lock expiration time for an entity if the user owns the lock.
    /// </summary>
    /// <param name="entity">The entity whose lock should be refreshed.</param>
    /// <param name="userId">The ID of the user owning the lock.</param>
    /// <remarks>
    /// This method extends the lock expiration time to prevent the lock from expiring
    /// during long-running operations. If the entity doesn't implement <see cref="ILockableEntity{TEntity}"/>
    /// or the user doesn't own the lock, this method does nothing.
    /// </remarks>
    public void RefreshLockIfOwned(TEntity entity, Guid userId)
    {
        if (entity is not ILockableEntity<TEntity> lockable) 
            return;
        
        logger.LogDebug("Refreshing lock for {Entity} with the Id: {EntityId} to User with the Id: {UserId}", typeof(TEntity).Name, entity.Id, userId);
        lockable.Locking.RefreshLock(userId);
        logger.LogInformation("{Entity} with the Id: {EntityId} lock successfully refreshed for User with the Id: {UserId}", typeof(TEntity).Name, entity.Id, userId);
    }
    
    /// <summary>
    /// Refreshes the lock expiration time for an entity and its child entities recursively.
    /// </summary>
    /// <param name="entity">The entity whose lock should be refreshed.</param>
    /// <param name="userId">The ID of the user owning the lock.</param>
    /// <param name="maxDepth">The maximum depth to traverse when refreshing children (default: 1).</param>
    /// <remarks>
    /// This method extends the lock expiration time for the entity and all child entities
    /// marked with <see cref="CascadeLockAttribute"/> up to the specified depth.
    /// </remarks>
    public void RefreshLockWithChildrenIfOwned(TEntity entity, Guid userId, int maxDepth = 1)
    {
        RefreshLockIfOwned(entity, userId);
        RefreshChildrenLocksRecursive(entity, userId, currentDepth: 0, maxDepth);
    }

    /// <summary>
    /// Checks if an entity is locked by a user other than the specified user.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <param name="userId">The ID of the user to check against.</param>
    /// <returns>
    /// True if the entity is locked by another user; 
    /// False if the entity is unlocked, locked by the specified user, or the lock has expired.
    /// </returns>
    /// <remarks>
    /// This method is useful for read-only operations to determine if modifications might be blocked.
    /// If the entity doesn't implement <see cref="ILockableEntity{TEntity}"/>, returns false.
    /// </remarks>
    public bool IsLockedByAnotherUser(TEntity entity, Guid userId)
    {
        if (entity is not ILockableEntity<TEntity> lockable)
        {
            logger.LogDebug("{Entity} with the Id: {EntityId} is not lockable", typeof(TEntity).Name, entity.Id);
            return false;
        }
        
        var lockInfo = lockable.Locking.LockInfo;
        if (lockInfo.LockedByUserId == null || lockInfo.IsExpired())
        {
            logger.LogDebug("{Entity} with the Id: {EntityId} is currently unlocked or the lock is expired.", typeof(TEntity).Name, entity.Id);
            return false;
        }

        var isNotLockedByUserId = !lockInfo.IsLockedBy(userId);
        if (isNotLockedByUserId)
        {
            logger.LogDebug("{Entity} with the Id: {EntityId} is locked by User {LockedByUserId}, not by User {CurrentUserId}", typeof(TEntity).Name, entity.Id, lockInfo.LockedByUserId, userId);
        }
        return isNotLockedByUserId;
    }
    
    /// <summary>
    /// Forcefully unlocks an entity regardless of lock ownership.
    /// This is an administrative operation that should be used with caution.
    /// </summary>
    /// <param name="entity">The entity to force unlock.</param>
    /// <remarks>
    /// This method bypasses normal lock ownership checks and immediately unlocks the entity.
    /// It's typically used for administrative purposes, such as clearing stale locks.
    /// If the entity doesn't implement <see cref="ILockableEntity{TEntity}"/>, this method does nothing.
    /// </remarks>
    public void ForceUnlockIfSupported(TEntity entity)
    {
        if (entity is not ILockableEntity<TEntity> lockable) 
            return;
        
        logger.LogWarning("{Entity} with the Id: {EntityId} will be forcefully unlocked", typeof(TEntity).Name, entity.Id);
        lockable.Locking.ForceUnlock();
        logger.LogInformation("{Entity} with the Id: {EntityId} successfully forcefully unlocked", typeof(TEntity).Name, entity.Id);
    }

    /// <summary>
    /// Forcefully unlocks an entity and its child entities recursively regardless of lock ownership.
    /// This is an administrative operation that should be used with caution.
    /// </summary>
    /// <param name="entity">The entity to force unlock.</param>
    /// <param name="maxDepth">The maximum depth to traverse when unlocking children (default: 1).</param>
    /// <remarks>
    /// This method bypasses normal lock ownership checks and immediately unlocks the entity
    /// and all child entities marked with <see cref="CascadeLockAttribute"/> up to the specified depth.
    /// </remarks>
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
        if (!IsLockableEntity(child))
            return;

        var childEntityType = child.GetType();
        var reflectionInfo = GetReflectionInfo(childEntityType);

        if (reflectionInfo.LockingProperty == null || reflectionInfo.LockMethod == null)
            return;

        var locking = reflectionInfo.LockingProperty.GetValue(child);
        if (locking == null) 
            return;

        try
        {
            logger.LogDebug("Locking child {Entity} to User with the ID {UserId}", childEntityType.Name, userId);
            reflectionInfo.LockMethod.Invoke(locking, [userId]);
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
        if (!IsLockableEntity(child))
            return;

        var childEntityType = child.GetType();
        var reflectionInfo = GetReflectionInfo(childEntityType);

        if (reflectionInfo.LockingProperty == null || reflectionInfo.UnlockMethod == null)
            return;

        var locking = reflectionInfo.LockingProperty.GetValue(child);
        if (locking == null) 
            return;

        logger.LogDebug("Unlocking child {Entity} from User with the ID {UserId}", childEntityType.Name, userId);
        reflectionInfo.UnlockMethod.Invoke(locking, [userId]);
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
        if (!IsLockableEntity(child))
            return;

        var childEntityType = child.GetType();
        var reflectionInfo = GetReflectionInfo(childEntityType);

        if (reflectionInfo.LockingProperty == null || reflectionInfo.LockInfoProperty == null || reflectionInfo.IdProperty == null)
            return;

        var locking = reflectionInfo.LockingProperty.GetValue(child);
        if (locking == null) 
            return;

        var lockInfo = reflectionInfo.LockInfoProperty.GetValue(locking) as LockInfo;
        if (lockInfo == null) 
            return;

        var entityId = reflectionInfo.IdProperty.GetValue(child) as Guid? ?? Guid.Empty;

        logger.LogDebug("Validating child {Entity} with the Id: {EntityId} can be updated by User with the Id: {UserId}", 
            childEntityType.Name, entityId, userId);

        if (lockInfo.LockedByUserId == null)
        {
            logger.LogWarning("Child {Entity} with the Id: {EntityId} has not been locked prior to update", 
                childEntityType.Name, entityId);
            throw new EntityUnlockedException(childEntityType.Name, entityId);
        }

        if (lockInfo.IsExpired())
        {
            var expiredAt = lockInfo.LockedAtUtc!.Value.AddMinutes(lockInfo.LockTimeoutMinutes);
            logger.LogWarning(
                "Child {Entity} with the Id: {EntityId} has an expired lock. Locked by User {LockedByUserId} at {LockedAtUtc:yyyy-MM-dd HH:mm:ss} UTC, expired at {ExpiredAtUtc:yyyy-MM-dd HH:mm:ss} UTC after {TimeoutMinutes} minutes. Attempted access by User {CurrentUserId}",
                childEntityType.Name, entityId, lockInfo.LockedByUserId.Value, lockInfo.LockedAtUtc.Value, expiredAt, lockInfo.LockTimeoutMinutes, userId);
            
            throw new EntityLockExpiredException(
                childEntityType.Name,
                entityId,
                lockInfo.LockedByUserId.Value,
                lockInfo.LockedAtUtc.Value,
                lockInfo.LockTimeoutMinutes);
        }

        if (lockInfo.IsLockedBy(userId))
        {
            logger.LogDebug("Child {Entity} with the Id: {EntityId} is locked to the User with the Id: {UserId}. Update can proceed", 
                childEntityType.Name, entityId, userId);
            ValidateChildrenLocksRecursive(child, userId, currentDepth + 1, maxDepth);
            return;
        }

        logger.LogWarning(
            "Child {Entity} with the Id: {EntityId} is locked by User {LockedByUserId} (locked at {LockedAtUtc:yyyy-MM-dd HH:mm:ss} UTC) and cannot be unlocked by User {CurrentUserId}",
            childEntityType.Name, entityId, lockInfo.LockedByUserId.Value, lockInfo.LockedAtUtc?.ToString("yyyy-MM-dd HH:mm:ss") ?? "unknown", userId);
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
        if (!IsLockableEntity(child))
            return;

        var childEntityType = child.GetType();
        var reflectionInfo = GetReflectionInfo(childEntityType);

        if (reflectionInfo.LockingProperty == null || reflectionInfo.RefreshLockMethod == null || reflectionInfo.IdProperty == null)
            return;

        var locking = reflectionInfo.LockingProperty.GetValue(child);
        if (locking == null) 
            return;

        var entityId = reflectionInfo.IdProperty.GetValue(child) as Guid? ?? Guid.Empty;

        logger.LogDebug("Refreshing lock for child {Entity} with the Id: {EntityId} to User with the Id: {UserId}", 
            childEntityType.Name, entityId, userId);
        reflectionInfo.RefreshLockMethod.Invoke(locking, [userId]);
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
        if (!IsLockableEntity(child))
            return;

        var childEntityType = child.GetType();
        var reflectionInfo = GetReflectionInfo(childEntityType);

        if (reflectionInfo.LockingProperty == null || reflectionInfo.ForceUnlockMethod == null || reflectionInfo.IdProperty == null)
            return;

        var locking = reflectionInfo.LockingProperty.GetValue(child);
        if (locking == null) 
            return;

        var entityId = reflectionInfo.IdProperty.GetValue(child) as Guid? ?? Guid.Empty;

        logger.LogWarning("Forcefully unlocking child {Entity} with the Id: {EntityId}", childEntityType.Name, entityId);
        reflectionInfo.ForceUnlockMethod.Invoke(locking, null);
        ForceUnlockChildrenRecursive(child, currentDepth + 1, maxDepth);
    }
}