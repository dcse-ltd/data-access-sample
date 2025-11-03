using Infrastructure.Entity.Interfaces;
using Infrastructure.Repository.Interfaces;
using Infrastructure.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Generic service for syncing child entity collections based on DTOs.
/// Supports soft deletion for entities implementing ISoftDeletableEntity
/// and only updates entities that have actually changed.
/// </summary>
public class CollectionSyncService(ILogger<CollectionSyncService> logger) 
    : ICollectionSyncService
{
    /// <summary>
    /// Syncs a collection of child entities with a collection of DTOs.
    /// - Removes/soft-deletes entities not in DTO collection
    /// - Updates only entities that have changed
    /// - Adds new entities from DTO collection
    /// </summary>
    /// <typeparam name="TChildEntity">The child entity type</typeparam>
    /// <typeparam name="TChildDto">The child DTO type</typeparam>
    /// <param name="existingChildren">The existing child entity collection</param>
    /// <param name="dtoChildren">The DTO collection to sync with</param>
    /// <param name="getDtoKey">Function to extract the key (Id) from a DTO</param>
    /// <param name="hasChanges">Function to determine if an entity has changed compared to its DTO</param>
    /// <param name="updateExisting">Action to update an existing entity from its DTO</param>
    /// <param name="createNew">Function to create a new entity from a DTO</param>
    /// <param name="childSoftDeleteService">Soft delete service for child entities (optional)</param>
    /// <param name="childRepository">Repository for child entities (optional, needed for hard delete)</param>
    /// <param name="userId">The current user ID for soft deletion</param>
    public void SyncChildCollection<TChildEntity, TChildDto>(
        ICollection<TChildEntity> existingChildren,
        IEnumerable<TChildDto> dtoChildren,
        Func<TChildDto, Guid> getDtoKey,
        Func<TChildEntity, TChildDto, bool> hasChanges,
        Action<TChildEntity, TChildDto> updateExisting,
        Func<TChildDto, TChildEntity> createNew,
        IEntitySoftDeleteService<TChildEntity>? childSoftDeleteService,
        IRepository<TChildEntity>? childRepository,
        Guid userId)
        where TChildEntity : class, IEntity
    {
        ArgumentNullException.ThrowIfNull(existingChildren);
        ArgumentNullException.ThrowIfNull(dtoChildren);
        ArgumentNullException.ThrowIfNull(getDtoKey);
        ArgumentNullException.ThrowIfNull(hasChanges);
        ArgumentNullException.ThrowIfNull(updateExisting);
        ArgumentNullException.ThrowIfNull(createNew);
        
        var dtoList = dtoChildren.ToList();

        // Remove/soft-delete entities that are no longer in the DTO collection
        // Only consider entities with valid IDs (not default/empty GUIDs)
        // This filters out any entities that haven't been persisted yet or are in a transitional state
        var childrenToRemove = existingChildren
            .Where(existing => IsValidEntityId(existing.Id) && 
                   !dtoList.Any(dto => getDtoKey(dto) == existing.Id))
            .ToList();

        foreach (var child in childrenToRemove)
        {
            if (child is ISoftDeletableEntity && childSoftDeleteService != null)
            {
                logger.LogDebug(
                    "Soft deleting child {EntityType} with Id: {EntityId}",
                    typeof(TChildEntity).Name,
                    child.Id);
                childSoftDeleteService.StampForDelete(child, userId);
            }
            else
            {
                logger.LogDebug(
                    "Removing child {EntityType} with Id: {EntityId} from collection",
                    typeof(TChildEntity).Name,
                    child.Id);
                existingChildren.Remove(child);
                
                if (childRepository != null)
                {
                    childRepository.Remove(child);
                }
            }
        }

        foreach (var dto in dtoList)
        {
            var dtoKey = getDtoKey(dto);
            var existing = existingChildren.FirstOrDefault(child => child.Id == dtoKey);

            if (existing != null)
            {
                if (existing is ISoftDeletableEntity softDeletableExisting && 
                    childSoftDeleteService != null &&
                    softDeletableExisting.Deleted.SoftDeleteInfo.IsDeleted)
                {
                    logger.LogDebug(
                        "Restoring soft-deleted child {EntityType} with Id: {EntityId}",
                        typeof(TChildEntity).Name,
                        existing.Id);
                    childSoftDeleteService.StampForRestore(existing);
                }
                
                if (hasChanges(existing, dto))
                {
                    logger.LogDebug(
                        "Updating child {EntityType} with Id: {EntityId}",
                        typeof(TChildEntity).Name,
                        existing.Id);
                    updateExisting(existing, dto);
                }
                else
                {
                    logger.LogDebug(
                        "Child {EntityType} with Id: {EntityId} has no changes, skipping update",
                        typeof(TChildEntity).Name,
                        existing.Id);
                }
            }
            else
            {
                // Generate a new ID if the DTO doesn't have one (empty or default GUID)
                if (!IsValidEntityId(dtoKey))
                {
                    dtoKey = Guid.NewGuid();
                }

                logger.LogDebug(
                    "Adding new child {EntityType} with Id: {EntityId}",
                    typeof(TChildEntity).Name,
                    dtoKey);
                
                var newChild = createNew(dto);
                newChild.Id = dtoKey;
                existingChildren.Add(newChild);
            }
        }
    }

    /// <summary>
    /// Determines if a GUID represents a valid entity ID (not empty or default).
    /// </summary>
    /// <param name="id">The GUID to validate.</param>
    /// <returns>True if the GUID is a valid entity ID; otherwise, false.</returns>
    /// <remarks>
    /// This method checks if the GUID is not the default/empty value.
    /// In C#, <c>default(Guid)</c> and <c>Guid.Empty</c> are equivalent (both represent 00000000-0000-0000-0000-000000000000).
    /// Entities with default/empty GUIDs are considered not yet persisted and should not be included
    /// in removal operations to avoid edge cases during entity creation and syncing.
    /// </remarks>
    private static bool IsValidEntityId(Guid id)
    {
        // Check both default and Empty for explicit clarity, though they are equivalent
        return id != default(Guid) && id != Guid.Empty;
    }
}

