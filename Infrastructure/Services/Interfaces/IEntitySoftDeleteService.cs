using Infrastructure.Entity.Interfaces;

namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Service contract for managing soft delete functionality on entities.
/// Provides methods to mark entities as deleted and restore previously soft-deleted entities.
/// Supports cascading soft delete operations for parent-child relationships.
/// </summary>
/// <typeparam name="TEntity">The entity type that implements <see cref="IEntity"/> and optionally <see cref="ISoftDeletableEntity"/>.</typeparam>
/// <remarks>
/// This interface provides soft delete management for entities that implement <see cref="ISoftDeletableEntity"/>.
/// Soft deletion marks entities as deleted without physically removing them from the database,
/// allowing for data recovery and audit trails.
/// </remarks>
public interface IEntitySoftDeleteService<in TEntity> 
    where TEntity : class, IEntity
{
    /// <summary>
    /// Marks an entity as soft-deleted by setting the deletion timestamp and user ID.
    /// </summary>
    /// <param name="entity">The entity to soft delete.</param>
    /// <param name="userId">The ID of the user performing the deletion.</param>
    void StampForDelete(
        TEntity entity, 
        Guid userId);
    
    /// <summary>
    /// Marks an entity and its child entities as soft-deleted recursively based on <see cref="CascadeDeleteAttribute"/>.
    /// </summary>
    /// <param name="entity">The entity to soft delete.</param>
    /// <param name="userId">The ID of the user performing the deletion.</param>
    /// <param name="maxDepth">The maximum depth to traverse when soft deleting children (default: 1).</param>
    void StampForDeleteWithChildren(
        TEntity entity, 
        Guid userId, 
        int maxDepth = 1);
    
    /// <summary>
    /// Restores a soft-deleted entity by clearing the deletion flag and resetting deletion metadata.
    /// </summary>
    /// <param name="entity">The entity to restore.</param>
    void StampForRestore(
        TEntity entity);
    
    /// <summary>
    /// Restores a soft-deleted entity and its child entities recursively based on <see cref="CascadeDeleteAttribute"/>.
    /// </summary>
    /// <param name="entity">The entity to restore.</param>
    /// <param name="maxDepth">The maximum depth to traverse when restoring children (default: 1).</param>
    void StampForRestoreWithChildren(
        TEntity entity, 
        int maxDepth = 1);
}