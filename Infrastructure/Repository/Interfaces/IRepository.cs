using Infrastructure.Entity.Interfaces;
using Infrastructure.Repository.Models;

namespace Infrastructure.Repository.Interfaces;

/// <summary>
/// Repository interface providing data access operations for entities.
/// Defines the contract for CRUD operations, querying, and entity management.
/// </summary>
/// <typeparam name="TEntity">The entity type that implements <see cref="IEntity"/>.</typeparam>
/// <remarks>
/// This interface provides a data access abstraction layer, allowing implementations
/// to support various data stores (e.g., Entity Framework Core, Dapper, etc.).
/// It supports:
/// <list type="bullet">
/// <item><description>Retrieval operations (GetById, GetAll, Find)</description></item>
/// <item><description>Write operations (Add, Update, Remove)</description></item>
/// <item><description>Query options for tracking and soft-deleted entity inclusion</description></item>
/// <item><description>Specification pattern for complex queries and includes</description></item>
/// </list>
/// </remarks>
public interface IRepository<TEntity> where TEntity : class, IEntity
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
    /// Retrieves all entities.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A collection of all entities.</returns>
    Task<IEnumerable<TEntity>> GetAllAsync(
        CancellationToken cancellationToken);
    
    /// <summary>
    /// Retrieves all entities with custom query options.
    /// </summary>
    /// <param name="queryOptions">Options for controlling tracking and soft-deleted entity inclusion.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A collection of entities.</returns>
    Task<IEnumerable<TEntity>> GetAllAsync(
        QueryOptions? queryOptions,
        CancellationToken cancellationToken);
    
    /// <summary>
    /// Adds a new entity to the repository for insertion.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <remarks>
    /// The entity is added to the change tracker but not saved until <see cref="IUnitOfWork.SaveChangesAsync"/> is called.
    /// </remarks>
    Task AddAsync(
        TEntity entity, 
        CancellationToken cancellationToken);
    
    /// <summary>
    /// Marks an entity for update in the repository.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <remarks>
    /// The entity is marked as modified in the change tracker but not saved until <see cref="IUnitOfWork.SaveChangesAsync"/> is called.
    /// All properties of the entity will be marked as modified.
    /// </remarks>
    void Update(
        TEntity entity);
    
    /// <summary>
    /// Marks an entity for removal from the repository.
    /// </summary>
    /// <param name="entity">The entity to remove.</param>
    /// <remarks>
    /// The entity is marked as deleted in the change tracker but not removed until <see cref="IUnitOfWork.SaveChangesAsync"/> is called.
    /// </remarks>
    void Remove(
        TEntity entity);
    
    /// <summary>
    /// Finds entities matching the specified specification.
    /// </summary>
    /// <param name="spec">The specification defining the query criteria, includes, ordering, and paging.</param>
    /// <param name="queryOptions">Options for controlling tracking and soft-deleted entity inclusion.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A collection of entities matching the specification.</returns>
    /// <remarks>
    /// The specification can include filtering criteria, related entity includes, ordering, and paging.
    /// For paginated results with total count information, use <see cref="FindPagedAsync"/> instead.
    /// </remarks>
    Task<IEnumerable<TEntity>> FindAsync(
        ISpecification<TEntity> spec, 
        QueryOptions? queryOptions,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Finds entities matching the specified specification and returns paginated results with metadata.
    /// </summary>
    /// <param name="spec">The specification defining the query criteria, includes, ordering, and paging.</param>
    /// <param name="queryOptions">Options for controlling tracking and soft-deleted entity inclusion.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="PagedResult{TEntity}"/> containing the items for the current page and pagination metadata.</returns>
    /// <remarks>
    /// <para>
    /// This method is similar to <see cref="FindAsync"/> but returns pagination metadata including:
    /// <list type="bullet">
    /// <item><description>Total count of items across all pages</description></item>
    /// <item><description>Current page number and page size</description></item>
    /// <item><description>Total pages</description></item>
    /// <item><description>Whether previous/next pages exist</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// The specification should include paging information via <see cref="BaseSpecification{TEntity}.ApplyPaging"/>.
    /// The page number and page size are extracted from the specification's Skip and Take values.
    /// </para>
    /// 
    /// <para>
    /// Usage example:
    /// <code>
    /// var spec = new FindCustomersSpecification(lastName: "Smith", page: 1, pageSize: 10);
    /// var result = await repository.FindPagedAsync(spec, QueryOptions.Default);
    /// 
    /// // Access items
    /// foreach (var customer in result.Items) { ... }
    /// 
    /// // Access pagination metadata
    /// Console.WriteLine($"Showing {result.Items.Count()} of {result.TotalCount} results");
    /// Console.WriteLine($"Page {result.Page} of {result.TotalPages}");
    /// </code>
    /// </para>
    /// </remarks>
    Task<PagedResult<TEntity>> FindPagedAsync(
        ISpecification<TEntity> spec, 
        QueryOptions? queryOptions,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Finds a single entity matching the specified specification, returning null if not found.
    /// </summary>
    /// <param name="spec">The specification defining the query criteria and includes.</param>
    /// <param name="queryOptions">Options for controlling tracking and soft-deleted entity inclusion.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The entity if found; otherwise, null.</returns>
    /// <remarks>
    /// This method is useful for finding a single entity by criteria other than ID.
    /// If multiple entities match, only the first one is returned.
    /// </remarks>
    Task<TEntity?> FindOneAsync(
        ISpecification<TEntity> spec, 
        QueryOptions? queryOptions,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Counts the number of entities matching the specified specification.
    /// </summary>
    /// <param name="spec">The specification defining the query criteria.</param>
    /// <param name="queryOptions">Options for controlling soft-deleted entity inclusion.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The count of entities matching the specification.</returns>
    /// <remarks>
    /// This method executes a count query, which is more efficient than retrieving all entities and counting them.
    /// </remarks>
    Task<int> CountAsync(
        ISpecification<TEntity> spec, 
        QueryOptions? queryOptions,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if an entity with the specified ID exists.
    /// </summary>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <param name="queryOptions">Options for controlling soft-deleted entity inclusion.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>True if an entity with the specified ID exists; otherwise, false.</returns>
    /// <remarks>
    /// This method executes an efficient existence check query without retrieving the full entity.
    /// </remarks>
    Task<bool> ExistsAsync(
        Guid id, 
        QueryOptions? queryOptions,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if any entities match the specified specification.
    /// </summary>
    /// <param name="spec">The specification defining the query criteria.</param>
    /// <param name="queryOptions">Options for controlling soft-deleted entity inclusion.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>True if any entities match the specification; otherwise, false.</returns>
    /// <remarks>
    /// This method executes an efficient existence check query without retrieving the full entities.
    /// </remarks>
    Task<bool> AnyAsync(
        ISpecification<TEntity> spec, 
        QueryOptions? queryOptions,
        CancellationToken cancellationToken = default);
}