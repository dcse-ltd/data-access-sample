using Infrastructure.Entity.Interfaces;
using Infrastructure.Repository.Interfaces;
using Infrastructure.Services.Interfaces;

namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Service contract for syncing child entity collections with DTOs.
/// Provides generic collection synchronization that handles adding, updating, and removing child entities
/// based on DTO collections, with support for soft deletion.
/// </summary>
/// <remarks>
/// This interface provides a generic way to synchronize parent entity child collections with DTO collections.
/// It supports:
/// <list type="bullet">
/// <item><description>Removing/soft-deleting entities not in the DTO collection</description></item>
/// <item><description>Updating only entities that have actually changed</description></item>
/// <item><description>Adding new entities from the DTO collection</description></item>
/// <item><description>Soft deletion for entities implementing <see cref="ISoftDeletableEntity"/></description></item>
/// <item><description>Restoring soft-deleted entities that appear in the DTO collection</description></item>
/// </list>
/// 
/// The service is designed to be used within update operations where a parent entity's child collection
/// needs to be synchronized with data from a DTO, typically received from a client application.
/// </remarks>
public interface ICollectionSyncService
{
    /// <summary>
    /// Syncs a collection of child entities with a collection of DTOs.
    /// </summary>
    /// <typeparam name="TChildEntity">The child entity type that implements <see cref="IEntity"/>.</typeparam>
    /// <typeparam name="TChildDto">The child DTO type.</typeparam>
    /// <param name="existingChildren">The existing child entity collection to sync.</param>
    /// <param name="dtoChildren">The DTO collection to sync with.</param>
    /// <param name="getDtoKey">Function to extract the key (Id) from a DTO.</param>
    /// <param name="hasChanges">Function to determine if an entity has changed compared to its DTO.</param>
    /// <param name="updateExisting">Action to update an existing entity from its DTO.</param>
    /// <param name="createNew">Function to create a new entity from a DTO.</param>
    /// <param name="childSoftDeleteService">Soft delete service for child entities (optional, required for soft deletion).</param>
    /// <param name="childRepository">Repository for child entities (optional, required for hard deletion).</param>
    /// <param name="userId">The current user ID for soft deletion operations.</param>
    /// <remarks>
    /// This method performs the following operations:
    /// <list type="number">
    /// <item><description>Removes or soft-deletes entities that are no longer in the DTO collection</description></item>
    /// <item><description>Restores soft-deleted entities if they appear in the DTO collection</description></item>
    /// <item><description>Updates existing entities only if they have changed (as determined by <paramref name="hasChanges"/>)</description></item>
    /// <item><description>Adds new entities from the DTO collection</description></item>
    /// </list>
    /// 
    /// If a child entity implements <see cref="ISoftDeletableEntity"/> and <paramref name="childSoftDeleteService"/>
    /// is provided, entities will be soft-deleted instead of removed. Otherwise, entities are removed from the collection.
    /// </remarks>
    void SyncChildCollection<TChildEntity, TChildDto>(
        ICollection<TChildEntity> existingChildren,
        IEnumerable<TChildDto> dtoChildren,
        Func<TChildDto, Guid> getDtoKey,
        Func<TChildEntity, TChildDto, bool> hasChanges,
        Action<TChildEntity, TChildDto> updateExisting,
        Func<TChildDto, TChildEntity> createNew,
        IEntitySoftDeleteService<TChildEntity>? childSoftDeleteService,
        IRepository<TChildEntity>? childRepository,
        Guid userId)
        where TChildEntity : class, IEntity;
}

