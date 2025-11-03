using System.Collections;
using System.Reflection;
using Infrastructure.Entity.Attributes;
using Infrastructure.Entity.Interfaces;
using Infrastructure.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Service for managing soft delete functionality on entities.
/// Provides methods to mark entities as deleted and restore previously soft-deleted entities.
/// Supports cascading soft delete operations for parent-child relationships.
/// </summary>
/// <typeparam name="TEntity">The entity type that implements <see cref="IEntity"/> and optionally <see cref="ISoftDeletableEntity"/>.</typeparam>
/// <remarks>
/// This service provides soft delete management for entities that implement <see cref="ISoftDeletableEntity"/>.
/// Soft deletion marks entities as deleted without physically removing them from the database,
/// allowing for data recovery and audit trails.
/// 
/// The service supports:
/// <list type="bullet">
/// <item><description>Soft deleting individual entities</description></item>
/// <item><description>Cascading soft delete to child entities via <see cref="CascadeDeleteAttribute"/></description></item>
/// <item><description>Restoring soft-deleted entities</description></item>
/// <item><description>Cascading restore to child entities</description></item>
/// </list>
/// 
/// Usage example:
/// <code>
/// // Soft delete an entity
/// entitySoftDeleteService.StampForDelete(entity, userId);
/// 
/// // Soft delete with cascading to children
/// entitySoftDeleteService.StampForDeleteWithChildren(entity, userId, maxDepth: 1);
/// 
/// // Restore a soft-deleted entity
/// entitySoftDeleteService.StampForRestore(entity);
/// 
/// // Restore with cascading to children
/// entitySoftDeleteService.StampForRestoreWithChildren(entity, maxDepth: 1);
/// </code>
/// 
/// Note: Soft-deleted entities are typically filtered out of queries by default.
/// To include soft-deleted entities in queries, use <see cref="QueryOptions"/> with 
/// <see cref="QueryOptions.IncludeSoftDeleted"/> set to true.
/// </remarks>
public class EntitySoftDeleteService<TEntity>(
    ILogger<EntitySoftDeleteService<TEntity>> logger
) : IEntitySoftDeleteService<TEntity>
    where TEntity : class, IEntity
{
    /// <summary>
    /// Marks an entity as soft-deleted by setting the deletion timestamp and user ID.
    /// </summary>
    /// <param name="entity">The entity to soft delete.</param>
    /// <param name="userId">The ID of the user performing the deletion.</param>
    /// <remarks>
    /// This method sets the soft delete flag and records who deleted the entity and when.
    /// If the entity doesn't implement <see cref="ISoftDeletableEntity"/>, this method does nothing.
    /// 
    /// Soft-deleted entities remain in the database but are typically filtered out of queries
    /// by default through EF Core query filters.
    /// </remarks>
    public void StampForDelete(TEntity entity, Guid userId)
    {
        if (entity is not ISoftDeletableEntity softDeletable)
        {
            logger.LogDebug("{Entity} with the Id: {EntityId} does not support soft delete", typeof(TEntity).Name, entity.Id);
            return;
        }

        logger.LogDebug("Soft deleting {Entity} with the Id: {EntityId} by User with the ID {UserId}", typeof(TEntity).Name, entity.Id, userId);
        softDeletable.Deleted.MarkSoftDeleted(userId);
        logger.LogInformation("{Entity} with the Id: {EntityId} successfully soft deleted by User with the ID {UserId}", typeof(TEntity).Name, entity.Id, userId);
    }

    /// <summary>
    /// Marks an entity and its child entities as soft-deleted recursively based on <see cref="CascadeDeleteAttribute"/>.
    /// </summary>
    /// <param name="entity">The entity to soft delete.</param>
    /// <param name="userId">The ID of the user performing the deletion.</param>
    /// <param name="maxDepth">The maximum depth to traverse when soft deleting children (default: 1).</param>
    /// <remarks>
    /// This method soft deletes the entity first, then recursively soft deletes all child entities
    /// marked with <see cref="CascadeDeleteAttribute"/> up to the specified depth.
    /// Useful when you want to ensure related child entities are also marked as deleted.
    /// </remarks>
    public void StampForDeleteWithChildren(TEntity entity, Guid userId, int maxDepth = 1)
    {
        StampForDelete(entity, userId);
        StampChildrenForDeleteRecursive(entity, userId, currentDepth: 0, maxDepth);
    }

    /// <summary>
    /// Restores a soft-deleted entity by clearing the deletion flag and resetting deletion metadata.
    /// </summary>
    /// <param name="entity">The entity to restore.</param>
    /// <remarks>
    /// This method clears the soft delete flag, allowing the entity to appear in normal queries again.
    /// If the entity doesn't implement <see cref="ISoftDeletableEntity"/>, this method does nothing.
    /// 
    /// After restoration, the entity will no longer be filtered out by default query filters.
    /// </remarks>
    public void StampForRestore(TEntity entity)
    {
        if (entity is not ISoftDeletableEntity softDeletable)
        {
            logger.LogDebug("{Entity} with the Id: {EntityId} does not support soft delete", typeof(TEntity).Name, entity.Id);
            return;
        }

        logger.LogDebug("Restoring {Entity} with the Id: {EntityId}", typeof(TEntity).Name, entity.Id);
        softDeletable.Deleted.Restore();
        logger.LogInformation("{Entity} with the Id: {EntityId} successfully restored", typeof(TEntity).Name, entity.Id);
    }

    /// <summary>
    /// Restores a soft-deleted entity and its child entities recursively based on <see cref="CascadeDeleteAttribute"/>.
    /// </summary>
    /// <param name="entity">The entity to restore.</param>
    /// <param name="maxDepth">The maximum depth to traverse when restoring children (default: 1).</param>
    /// <remarks>
    /// This method restores the entity first, then recursively restores all child entities
    /// marked with <see cref="CascadeDeleteAttribute"/> up to the specified depth.
    /// Useful when restoring a parent entity and ensuring all related child entities are also restored.
    /// </remarks>
    public void StampForRestoreWithChildren(TEntity entity, int maxDepth = 1)
    {
        StampForRestore(entity);
        StampChildrenForRestoreRecursive(entity, currentDepth: 0, maxDepth);
    }
    
    private void StampChildrenForDeleteRecursive(object entity, Guid userId, int currentDepth, int maxDepth)
    {
        if (currentDepth >= maxDepth) return;

        var entityType = entity.GetType();
        var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            if (property.GetCustomAttribute<CascadeDeleteAttribute>() == null)
                continue;

            var value = property.GetValue(entity);
            if (value == null)
                continue;

            if (value is IEnumerable enumerable and not string)
            {
                foreach (var child in enumerable)
                {
                    StampChildForDelete(child, userId, currentDepth, maxDepth);
                }
            }
            else
            {
                StampChildForDelete(value, userId, currentDepth, maxDepth);
            }
        }
    }

    private void StampChildForDelete(object child, Guid userId, int currentDepth, int maxDepth)
    {
        if (child is not ISoftDeletableEntity softDeletableChild)
            return;

        var childEntityType = child.GetType();
        var entityIdProperty = childEntityType.GetProperty("Id");
        var entityId = entityIdProperty?.GetValue(child) as Guid? ?? Guid.Empty;

        logger.LogDebug("Soft deleting child {Entity} with the Id: {EntityId} by User with the ID {UserId}", 
            childEntityType.Name, entityId, userId);

        softDeletableChild.Deleted.MarkSoftDeleted(userId);
        
        StampChildrenForDeleteRecursive(child, userId, currentDepth + 1, maxDepth);
    }

    private void StampChildrenForRestoreRecursive(object entity, int currentDepth, int maxDepth)
    {
        if (currentDepth >= maxDepth) return;

        var entityType = entity.GetType();
        var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            if (property.GetCustomAttribute<CascadeDeleteAttribute>() == null)
                continue;

            var value = property.GetValue(entity);
            if (value == null)
                continue;

            if (value is IEnumerable enumerable and not string)
            {
                foreach (var child in enumerable)
                {
                    StampChildForRestore(child, currentDepth, maxDepth);
                }
            }
            else
            {
                StampChildForRestore(value, currentDepth, maxDepth);
            }
        }
    }

    private void StampChildForRestore(object child, int currentDepth, int maxDepth)
    {
        if (child is not ISoftDeletableEntity softDeletableChild)
            return;

        var childEntityType = child.GetType();
        var entityIdProperty = childEntityType.GetProperty("Id");
        var entityId = entityIdProperty?.GetValue(child) as Guid? ?? Guid.Empty;

        logger.LogDebug("Restoring child {Entity} with the Id: {EntityId}", 
            childEntityType.Name, entityId);

        softDeletableChild.Deleted.Restore();
        
        StampChildrenForRestoreRecursive(child, currentDepth + 1, maxDepth);
    }
}