using Infrastructure.Entity.Interfaces;

namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Service contract for managing entity locking functionality, including locking, unlocking, validation, and refresh operations.
/// Supports both single entity locking and cascading locks for parent-child relationships.
/// </summary>
/// <typeparam name="TEntity">The entity type that implements <see cref="IEntity"/> and optionally <see cref="ILockableEntity{TEntity}"/>.</typeparam>
/// <remarks>
/// This interface provides comprehensive lock management for entities that implement <see cref="ILockableEntity{TEntity}"/>.
/// Lock validation ensures that entities are properly locked before modification and that locks belong to the current user.
/// </remarks>
public interface IEntityLockService<in TEntity> 
    where TEntity : class, IEntity
{
    /// <summary>
    /// Locks an entity for the specified user if the entity supports locking.
    /// </summary>
    /// <param name="entity">The entity to lock.</param>
    /// <param name="userId">The ID of the user locking the entity.</param>
    void LockIfSupported(TEntity entity, Guid userId);
    
    /// <summary>
    /// Locks an entity and its child entities recursively based on <see cref="CascadeLockAttribute"/>.
    /// </summary>
    /// <param name="entity">The entity to lock.</param>
    /// <param name="userId">The ID of the user locking the entity.</param>
    /// <param name="maxDepth">The maximum depth to traverse when locking children (default: 1).</param>
    void LockWithChildrenIfSupported(TEntity entity, Guid userId, int maxDepth = 1);
    
    /// <summary>
    /// Unlocks an entity for the specified user if the entity supports locking and the user owns the lock.
    /// </summary>
    /// <param name="entity">The entity to unlock.</param>
    /// <param name="userId">The ID of the user unlocking the entity.</param>
    /// <returns>True if the entity was unlocked or doesn't support locking; false if the user doesn't own the lock.</returns>
    bool UnlockIfSupported(TEntity entity, Guid userId);
    
    /// <summary>
    /// Unlocks an entity and its child entities recursively based on <see cref="CascadeLockAttribute"/>.
    /// </summary>
    /// <param name="entity">The entity to unlock.</param>
    /// <param name="userId">The ID of the user unlocking the entity.</param>
    /// <param name="maxDepth">The maximum depth to traverse when unlocking children (default: 1).</param>
    /// <returns>True if the entity was unlocked or doesn't support locking; false if the user doesn't own the lock.</returns>
    bool UnlockWithChildrenIfSupported(TEntity entity, Guid userId, int maxDepth = 1);
    
    /// <summary>
    /// Validates that an entity is locked and can be updated by the specified user.
    /// Throws an exception if the entity is not locked, locked by another user, or the lock has expired.
    /// </summary>
    /// <param name="entity">The entity to validate.</param>
    /// <param name="userId">The ID of the user attempting to update.</param>
    /// <exception cref="EntityUnlockedException">Thrown if the entity is not locked before the update operation.</exception>
    /// <exception cref="EntityLockExpiredException">Thrown if the entity's lock has expired.</exception>
    /// <exception cref="EntityLockedException">Thrown if the entity is locked by a different user.</exception>
    void ValidateLockForUpdate(TEntity entity, Guid userId);
    
    /// <summary>
    /// Validates that an entity and its child entities are locked and can be updated by the specified user.
    /// Recursively validates locks on child entities marked with <see cref="CascadeLockAttribute"/>.
    /// </summary>
    /// <param name="entity">The entity to validate.</param>
    /// <param name="userId">The ID of the user attempting to update.</param>
    /// <param name="maxDepth">The maximum depth to traverse when validating children (default: 1).</param>
    /// <exception cref="EntityUnlockedException">Thrown if the entity or any child entity is not locked before the update operation.</exception>
    /// <exception cref="EntityLockExpiredException">Thrown if the entity's or any child entity's lock has expired.</exception>
    /// <exception cref="EntityLockedException">Thrown if the entity or any child entity is locked by a different user.</exception>
    void ValidateLockForUpdateWithChildren(TEntity entity, Guid userId, int maxDepth = 1);
    
    /// <summary>
    /// Refreshes the lock expiration time for an entity if the user owns the lock.
    /// </summary>
    /// <param name="entity">The entity whose lock should be refreshed.</param>
    /// <param name="userId">The ID of the user owning the lock.</param>
    void RefreshLockIfOwned(TEntity entity, Guid userId);
    
    /// <summary>
    /// Refreshes the lock expiration time for an entity and its child entities recursively.
    /// </summary>
    /// <param name="entity">The entity whose lock should be refreshed.</param>
    /// <param name="userId">The ID of the user owning the lock.</param>
    /// <param name="maxDepth">The maximum depth to traverse when refreshing children (default: 1).</param>
    void RefreshLockWithChildrenIfOwned(TEntity entity, Guid userId, int maxDepth = 1);
    
    /// <summary>
    /// Checks if an entity is locked by a user other than the specified user.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <param name="userId">The ID of the user to check against.</param>
    /// <returns>
    /// True if the entity is locked by another user; 
    /// False if the entity is unlocked, locked by the specified user, or the lock has expired.
    /// </returns>
    bool IsLockedByAnotherUser(TEntity entity, Guid userId);
    
    /// <summary>
    /// Forcefully unlocks an entity regardless of lock ownership.
    /// This is an administrative operation that should be used with caution.
    /// </summary>
    /// <param name="entity">The entity to force unlock.</param>
    void ForceUnlockIfSupported(TEntity entity);
    
    /// <summary>
    /// Forcefully unlocks an entity and its child entities recursively regardless of lock ownership.
    /// This is an administrative operation that should be used with caution.
    /// </summary>
    /// <param name="entity">The entity to force unlock.</param>
    /// <param name="maxDepth">The maximum depth to traverse when unlocking children (default: 1).</param>
    void ForceUnlockWithChildrenIfSupported(TEntity entity, int maxDepth = 1);
}