using System.Collections;
using System.Reflection;
using Infrastructure.Entity.Attributes;
using Infrastructure.Entity.Interfaces;
using Infrastructure.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class EntitySoftDeleteService<TEntity>(
    ILogger<EntitySoftDeleteService<TEntity>> logger
) : IEntitySoftDeleteService<TEntity>
    where TEntity : class, IEntity
{
    public void StampForDelete(TEntity entity, Guid userId)
    {
        if (entity is not ISoftDeletableEntity softDeletable)
        {
            logger.LogInformation("{Entity} with the Id: {EntityId} does not support soft delete", typeof(TEntity).Name, entity.Id);
            return;
        }

        logger.LogInformation("Soft deleting {Entity} with the Id: {EntityId} by User with the ID {UserId}", typeof(TEntity).Name, entity.Id, userId);
        
        softDeletable.Deleted.MarkSoftDeleted(userId);
    }

    public void StampForDeleteWithChildren(TEntity entity, Guid userId, int maxDepth = 1)
    {
        StampForDelete(entity, userId);
        StampChildrenForDeleteRecursive(entity, userId, currentDepth: 0, maxDepth);
    }

    public void StampForRestore(TEntity entity)
    {
        if (entity is not ISoftDeletableEntity softDeletable)
        {
            logger.LogInformation("{Entity} with the Id: {EntityId} does not support soft delete", typeof(TEntity).Name, entity.Id);
            return;
        }

        logger.LogInformation("Restoring {Entity} with the Id: {EntityId}", typeof(TEntity).Name, entity.Id);
        
        softDeletable.Deleted.Restore();
    }

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

        logger.LogInformation("Soft deleting child {Entity} with the Id: {EntityId} by User with the ID {UserId}", 
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

        logger.LogInformation("Restoring child {Entity} with the Id: {EntityId}", 
            childEntityType.Name, entityId);

        softDeletableChild.Deleted.Restore();
        
        StampChildrenForRestoreRecursive(child, currentDepth + 1, maxDepth);
    }
}