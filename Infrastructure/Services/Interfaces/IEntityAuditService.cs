using Infrastructure.Entity.Interfaces;

namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Service contract for managing audit information (created/modified tracking) on entities.
/// Provides methods to stamp entities with creation and modification timestamps and user IDs.
/// </summary>
/// <typeparam name="TEntity">The entity type that implements <see cref="IEntity"/> and optionally <see cref="IAuditableEntity"/>.</typeparam>
/// <remarks>
/// Implementations of this interface automatically track when entities are created or modified
/// and by whom. Only entities implementing <see cref="IAuditableEntity"/> will be stamped;
/// other entities are silently ignored.
/// </remarks>
public interface IEntityAuditService<in TEntity> 
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
    /// </remarks>
    void StampForCreate(TEntity entity, Guid userId);
    
    /// <summary>
    /// Stamps an entity with modification audit information.
    /// Updates the ModifiedAt timestamp and records the user ID.
    /// </summary>
    /// <param name="entity">The entity to stamp.</param>
    /// <param name="userId">The ID of the user modifying the entity.</param>
    /// <remarks>
    /// This method is typically called when updating an existing entity.
    /// It updates only the modification timestamp; creation information remains unchanged.
    /// </remarks>
    void StampForUpdate(TEntity entity, Guid userId);
}