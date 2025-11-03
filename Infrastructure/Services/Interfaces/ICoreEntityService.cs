using Infrastructure.Entity.Interfaces;
using Infrastructure.Repository.Interfaces;
using Infrastructure.Repository.Models;
using Infrastructure.Services.Models;

namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Core service interface providing comprehensive CRUD operations for entities with support for locking, auditing, concurrency, and soft deletion.
/// Orchestrates multiple specialized services to provide a complete entity management solution.
/// </summary>
/// <typeparam name="TEntity">The entity type that implements <see cref="IEntity"/>.</typeparam>
/// <remarks>
/// This interface provides a unified contract for entity operations, combining data access, locking, auditing,
/// concurrency handling, and soft deletion functionality. Implementations automatically handle:
/// <list type="bullet">
/// <item><description>Lock validation before updates/deletes</description></item>
/// <item><description>Lock unlocking after operations</description></item>
/// <item><description>Audit stamping for create/update operations</description></item>
/// <item><description>Concurrency exception handling with row version tracking</description></item>
/// <item><description>Soft delete for entities that support it</description></item>
/// </list>
/// </remarks>
public interface ICoreEntityService<TEntity> where TEntity : class, IEntity
{
    /// <summary>
    /// Retrieves an entity by its ID, returning null if not found.
    /// </summary>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The entity if found; otherwise, null.</returns>
    Task<TEntity?> GetByIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves an entity by its ID with custom query options, returning null if not found.
    /// </summary>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <param name="queryOptions">Options for controlling tracking and soft-deleted entity inclusion.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The entity if found; otherwise, null.</returns>
    Task<TEntity?> GetByIdAsync(
        Guid id, 
        QueryOptions? queryOptions, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves an entity by its ID with custom query options and specification for includes, returning null if not found.
    /// </summary>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <param name="queryOptions">Options for controlling tracking and soft-deleted entity inclusion.</param>
    /// <param name="specification">Optional specification for including related entities.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The entity if found; otherwise, null.</returns>
    /// <remarks>
    /// Use the specification parameter to include related entities (e.g., child collections) when retrieving.
    /// This is useful for update operations where child entities need to be loaded for synchronization.
    /// </remarks>
    Task<TEntity?> GetByIdAsync(
        Guid id, 
        QueryOptions? queryOptions,
        ISpecification<TEntity>? specification,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves an entity by its ID, throwing an exception if not found.
    /// </summary>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The entity.</returns>
    /// <exception cref="EntityNotFoundException">Thrown if the entity is not found.</exception>
    Task<TEntity> GetByIdOrThrowAsync(
        Guid id, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves an entity by its ID with custom query options, throwing an exception if not found.
    /// </summary>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <param name="queryOptions">Options for controlling tracking and soft-deleted entity inclusion.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The entity.</returns>
    /// <exception cref="EntityNotFoundException">Thrown if the entity is not found.</exception>
    Task<TEntity> GetByIdOrThrowAsync(
        Guid id, 
        QueryOptions? queryOptions, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves an entity by its ID with custom query options and specification for includes, throwing an exception if not found.
    /// </summary>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <param name="queryOptions">Options for controlling tracking and soft-deleted entity inclusion.</param>
    /// <param name="specification">Optional specification for including related entities.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The entity.</returns>
    /// <exception cref="EntityNotFoundException">Thrown if the entity is not found.</exception>
    /// <remarks>
    /// Use the specification parameter to include related entities (e.g., child collections) when retrieving.
    /// </remarks>
    Task<TEntity> GetByIdOrThrowAsync(
        Guid id, 
        QueryOptions? queryOptions,
        ISpecification<TEntity>? specification,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves an entity by its ID and locks it for the current user.
    /// </summary>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The locked entity.</returns>
    /// <exception cref="EntityNotFoundException">Thrown if the entity is not found.</exception>
    Task<TEntity> GetByIdWithLockAsync(
        Guid id, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves an entity by its ID and locks it (and optionally child entities) for the current user.
    /// </summary>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <param name="lockOptions">Options for controlling lock behavior, including cascading locks to children.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The locked entity.</returns>
    /// <exception cref="EntityNotFoundException">Thrown if the entity is not found.</exception>
    Task<TEntity> GetByIdWithLockAsync(
        Guid id, 
        LockOptions? lockOptions, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Refreshes the lock expiration time for an entity owned by the current user.
    /// </summary>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="EntityNotFoundException">Thrown if the entity is not found.</exception>
    Task RefreshLockAsync(
        Guid id, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves all entities.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A collection of all entities.</returns>
    Task<IEnumerable<TEntity>> GetAllAsync(
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves all entities with custom query options.
    /// </summary>
    /// <param name="queryOptions">Options for controlling tracking and soft-deleted entity inclusion.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A collection of entities.</returns>
    Task<IEnumerable<TEntity>> GetAllAsync(
        QueryOptions? queryOptions,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Finds entities matching the specified specification.
    /// </summary>
    /// <param name="specification">The specification defining the query criteria and includes.</param>
    /// <param name="queryOptions">Options for controlling tracking and soft-deleted entity inclusion.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A collection of entities matching the specification.</returns>
    Task<IEnumerable<TEntity>> FindAsync(
        ISpecification<TEntity> specification,
        QueryOptions? queryOptions,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new entity, generating an ID and stamping audit information.
    /// </summary>
    /// <param name="entity">The entity to create.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created entity with generated ID and audit information.</returns>
    Task<TEntity> CreateAsync(
        TEntity entity, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an entity by applying the provided update action.
    /// </summary>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <param name="updateAction">The action to modify the entity.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated entity.</returns>
    /// <exception cref="EntityNotFoundException">Thrown if the entity is not found.</exception>
    Task<TEntity> UpdateAsync(
        Guid id, 
        Action<TEntity> updateAction, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an entity with lock validation and unlocking support.
    /// </summary>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <param name="updateAction">The action to modify the entity.</param>
    /// <param name="lockOptions">Options for controlling lock validation and unlocking (including children).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated entity.</returns>
    /// <exception cref="EntityNotFoundException">Thrown if the entity is not found.</exception>
    /// <exception cref="EntityUnlockedException">Thrown if the entity is not locked or locked by another user.</exception>
    Task<TEntity> UpdateAsync(
        Guid id, 
        Action<TEntity> updateAction, 
        LockOptions? lockOptions, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an entity with lock validation, unlocking support, and specification for including related entities.
    /// </summary>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <param name="updateAction">The action to modify the entity and optionally sync child collections.</param>
    /// <param name="lockOptions">Options for controlling lock validation and unlocking (including children).</param>
    /// <param name="specification">Optional specification for including related entities (e.g., child collections).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated entity.</returns>
    /// <exception cref="EntityNotFoundException">Thrown if the entity is not found.</exception>
    /// <exception cref="EntityUnlockedException">Thrown if the entity is not locked or locked by another user.</exception>
    /// <remarks>
    /// Use the specification parameter to include related entities when retrieving for update operations.
    /// This is essential when the updateAction needs to modify child collections (e.g., using <see cref="ICollectionSyncService"/>).
    /// </remarks>
    Task<TEntity> UpdateAsync(
        Guid id, 
        Action<TEntity> updateAction, 
        LockOptions? lockOptions,
        ISpecification<TEntity>? specification,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes an entity (soft delete if supported, otherwise hard delete).
    /// </summary>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="EntityNotFoundException">Thrown if the entity is not found.</exception>
    Task DeleteAsync(
        Guid id, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes an entity with lock validation (soft delete if supported, otherwise hard delete).
    /// </summary>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <param name="lockOptions">Options for controlling lock validation (including children).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="EntityNotFoundException">Thrown if the entity is not found.</exception>
    /// <exception cref="EntityUnlockedException">Thrown if the entity is not locked or locked by another user.</exception>
    Task DeleteAsync(
        Guid id, 
        LockOptions? lockOptions, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Permanently deletes an entity from the database, bypassing soft delete.
    /// </summary>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="EntityNotFoundException">Thrown if the entity is not found.</exception>
    Task HardDeleteAsync(
        Guid id, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Permanently deletes an entity from the database with lock validation, bypassing soft delete.
    /// </summary>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <param name="lockOptions">Options for controlling lock validation (including children).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="EntityNotFoundException">Thrown if the entity is not found.</exception>
    /// <exception cref="EntityUnlockedException">Thrown if the entity is not locked or locked by another user.</exception>
    Task HardDeleteAsync(
        Guid id, 
        LockOptions? lockOptions, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Restores a soft-deleted entity (and optionally its children).
    /// </summary>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <param name="includeChildren">Whether to also restore child entities marked with <see cref="CascadeDeleteAttribute"/>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="EntityNotFoundException">Thrown if the entity is not found.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the entity doesn't support soft delete.</exception>
    Task RestoreAsync(
        Guid id, 
        bool includeChildren, 
        CancellationToken cancellationToken = default);
}