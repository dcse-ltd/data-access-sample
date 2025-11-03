using Infrastructure.Entity.Interfaces;
using Infrastructure.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Service for managing audit information (created/modified tracking) on entities.
/// Provides methods to stamp entities with creation and modification timestamps and user IDs.
/// </summary>
/// <typeparam name="TEntity">The entity type that implements <see cref="IEntity"/> and optionally <see cref="IAuditableEntity"/>.</typeparam>
/// <remarks>
/// This service is typically used internally by <see cref="CoreEntityService{TEntity}"/> 
/// to automatically track when entities are created or modified and by whom.
/// 
/// Only entities implementing <see cref="IAuditableEntity"/> will be stamped;
/// other entities are silently ignored.
/// 
/// Usage example:
/// <code>
/// // In CoreEntityService.CreateAsync
/// entityAuditService.StampForCreate(entity, userId);
/// 
/// // In CoreEntityService.UpdateAsync
/// entityAuditService.StampForUpdate(entity, userId);
/// </code>
/// </remarks>
public class EntityAuditService<TEntity>(
    ILogger<EntityAuditService<TEntity>> logger
) : IEntityAuditService<TEntity>
    where TEntity : class, IEntity
{
    /// <summary>
    /// Stamps an entity with creation and modification audit information.
    /// Sets both CreatedAt and ModifiedAt timestamps, and records the user ID.
    /// </summary>
    /// <param name="entity">The entity to stamp.</param>
    /// <param name="userId">The ID of the user creating the entity.</param>
    /// <remarks>
    /// This method is typically called when creating a new entity.
    /// It sets both the creation and modification timestamps to the current UTC time,
    /// since a newly created entity is also considered modified at creation time.
    /// 
    /// If the entity doesn't implement <see cref="IAuditableEntity"/>, this method does nothing.
    /// </remarks>
    public void StampForCreate(TEntity entity, Guid userId)
    {
        if (entity is not IAuditableEntity auditable) 
            return;
        
        logger.LogDebug("Stamping {Entity} with the Id: {EntityId} for creation by User with the ID {UserId}", typeof(TEntity).Name, entity.Id, userId);
        auditable.Auditing.MarkCreated(userId);
        auditable.Auditing.MarkModified(userId);
        logger.LogDebug("{Entity} with the Id: {EntityId} successfully stamped for creation", typeof(TEntity).Name, entity.Id);
    }

    /// <summary>
    /// Stamps an entity with modification audit information.
    /// Updates the ModifiedAt timestamp and records the user ID.
    /// </summary>
    /// <param name="entity">The entity to stamp.</param>
    /// <param name="userId">The ID of the user modifying the entity.</param>
    /// <remarks>
    /// This method is typically called when updating an existing entity.
    /// It updates only the modification timestamp; creation information remains unchanged.
    /// 
    /// If the entity doesn't implement <see cref="IAuditableEntity"/>, this method does nothing.
    /// </remarks>
    public void StampForUpdate(TEntity entity, Guid userId)
    {
        if (entity is not IAuditableEntity auditable) 
            return;
        
        logger.LogDebug("Stamping {Entity} with the Id: {EntityId} for update by User with the ID {UserId}", typeof(TEntity).Name, entity.Id, userId);
        auditable.Auditing.MarkModified(userId);
        logger.LogDebug("{Entity} with the Id: {EntityId} successfully stamped for update", typeof(TEntity).Name, entity.Id);
    }
}