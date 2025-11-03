using Infrastructure.Context;
using Infrastructure.Entity.Interfaces;
using Infrastructure.Exceptions;
using Infrastructure.Repository.Interfaces;
using Infrastructure.Repository.Models;
using Infrastructure.Repository.Specification;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository;

/// <summary>
/// Repository implementation providing data access operations for entities using Entity Framework Core.
/// Implements the <see cref="IRepository{TEntity}"/> interface with EF Core-specific logic.
/// </summary>
/// <typeparam name="TEntity">The entity type that implements <see cref="IEntity"/>.</typeparam>
/// <remarks>
/// This repository provides an implementation of <see cref="IRepository{TEntity}"/> using
/// Entity Framework Core for data access. It handles:
/// <list type="bullet">
/// <item><description>Entity retrieval operations</description></item>
/// <item><description>Change tracking configuration</description></item>
/// <item><description>Soft-deleted entity filtering</description></item>
/// <item><description>Specification-based querying with includes</description></item>
/// <item><description>Entity modification operations (Add, Update, Remove)</description></item>
/// </list>
/// 
/// <para>
/// The repository uses <see cref="SpecificationEvaluator"/> to apply specifications to queries,
/// enabling complex queries with includes, ordering, and paging.
/// </para>
/// 
/// <para>
/// Note: This repository does not save changes to the database. Changes are tracked by EF Core
/// and must be saved using <see cref="IUnitOfWork.SaveChangesAsync"/>.
/// </para>
/// </remarks>
public class Repository<TEntity>(
    BaseAppDbContext context
    ) : IRepository<TEntity>
    where TEntity : class, IEntity
{
    private readonly DbSet<TEntity> _dbSet = context.Set<TEntity>();

    public async Task<TEntity?> GetByIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await GetByIdAsync(id, QueryOptions.Default, cancellationToken);
    }

    public async Task<TEntity?> GetByIdAsync(
        Guid id, 
        QueryOptions? queryOptions, 
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await GetByIdAsync(id, queryOptions, null, cancellationToken);
    }
    
    public async Task<TEntity?> GetByIdAsync(
        Guid id, 
        QueryOptions? queryOptions,
        ISpecification<TEntity>? specification,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        queryOptions ??= QueryOptions.Default;
        
        var baseQuery = _dbSet.AsQueryable();
        
        if (queryOptions.IncludeSoftDeleted)
        {
            baseQuery = baseQuery.IgnoreQueryFilters();
        }
        
        var query = specification != null 
            ? SpecificationEvaluator.GetQuery(baseQuery, specification)
            : baseQuery;
        query = query.Where(e => e.Id == id);
        
        query = ApplyTracking(query, queryOptions, specification);
        
        return await query.FirstOrDefaultAsync(cancellationToken);
    }
    
    public async Task<TEntity> GetByIdOrThrowAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await GetByIdOrThrowAsync(id, QueryOptions.Default, cancellationToken);
    }
    
    public async Task<TEntity> GetByIdOrThrowAsync(
        Guid id, 
        QueryOptions? queryOptions,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await GetByIdOrThrowAsync(id, queryOptions, null, cancellationToken);
    }
    
    public async Task<TEntity> GetByIdOrThrowAsync(
        Guid id, 
        QueryOptions? queryOptions,
        ISpecification<TEntity>? specification,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        queryOptions ??= QueryOptions.Default;
        
        var baseQuery = _dbSet.AsQueryable();
        
        if (queryOptions.IncludeSoftDeleted)
        {
            baseQuery = baseQuery.IgnoreQueryFilters();
        }
        
        var query = specification != null 
            ? SpecificationEvaluator.GetQuery(baseQuery, specification)
            : baseQuery;
        query = query.Where(e => e.Id == id);
        
        query = ApplyTracking(query, queryOptions, specification);
        
        var entity = await query.FirstOrDefaultAsync(cancellationToken);
        
        return entity ?? throw new EntityNotFoundException(typeof(TEntity).Name, id);
    }
    
    public async Task<IEnumerable<TEntity>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await GetAllAsync(QueryOptions.Default, cancellationToken);
    }

    public async Task<IEnumerable<TEntity>> GetAllAsync(
        QueryOptions? queryOptions, 
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        queryOptions ??= QueryOptions.Default;
        
        var query = ApplyQueryOptions(_dbSet.AsQueryable(), queryOptions, null);
        return await query.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        TEntity entity,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _dbSet.AddAsync(entity, cancellationToken);
    }

    public void Update(
        TEntity entity)
    {   
        _dbSet.Update(entity);
    }

    public void Remove(
        TEntity entity)
        => _dbSet.Remove(entity);

    public async Task<IEnumerable<TEntity>> FindAsync(
        ISpecification<TEntity> spec, 
        QueryOptions? queryOptions,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        queryOptions ??= QueryOptions.Default;
        
        var baseQuery = _dbSet.AsQueryable();
        if (queryOptions.IncludeSoftDeleted)
        {
            baseQuery = baseQuery.IgnoreQueryFilters();
        }
        
        var query = SpecificationEvaluator.GetQuery(baseQuery, spec);
        query = ApplyTracking(query, queryOptions, spec);
        return await query.ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<TEntity>> FindPagedAsync(
        ISpecification<TEntity> spec, 
        QueryOptions? queryOptions,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        queryOptions ??= QueryOptions.Default;
        
        var baseQuery = _dbSet.AsQueryable();
        if (queryOptions.IncludeSoftDeleted)
        {
            baseQuery = baseQuery.IgnoreQueryFilters();
        }
        
        // Get total count without pagination (count query doesn't need Skip/Take, Includes, or OrderBy)
        // Only apply criteria to get accurate count
        var countQuery = baseQuery;
        if (spec.Criteria != null)
        {
            countQuery = countQuery.Where(spec.Criteria);
        }
        // Count queries don't need includes or ordering - EF Core optimizes this
        var totalCount = await countQuery.CountAsync(cancellationToken);
        
        // Get paginated items
        var itemsQuery = SpecificationEvaluator.GetQuery(baseQuery, spec);
        itemsQuery = ApplyTracking(itemsQuery, queryOptions, spec);
        var items = await itemsQuery.ToListAsync(cancellationToken);
        
        // Extract page and pageSize from specification
        // Page is calculated from Skip: if Skip = 0 and Take = 10, page = 1
        // If Skip = 10 and Take = 10, page = 2, etc.
        int page;
        int pageSize;
        
        if (spec.Skip.HasValue && spec.Take.HasValue && spec.Take.Value > 0)
        {
            pageSize = spec.Take.Value;
            page = (spec.Skip.Value / pageSize) + 1;
        }
        else
        {
            // No pagination specified - treat as single page with all items
            pageSize = items.Count > 0 ? items.Count : 1;
            page = 1;
        }
        
        return new PagedResult<TEntity>(items, totalCount, page, pageSize);
    }

    public async Task<TEntity?> FindOneAsync(
        ISpecification<TEntity> spec, 
        QueryOptions? queryOptions,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        queryOptions ??= QueryOptions.Default;
        
        var baseQuery = _dbSet.AsQueryable();
        if (queryOptions.IncludeSoftDeleted)
        {
            baseQuery = baseQuery.IgnoreQueryFilters();
        }
        
        var query = SpecificationEvaluator.GetQuery(baseQuery, spec);
        query = ApplyTracking(query, queryOptions, spec);
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> CountAsync(
        ISpecification<TEntity> spec, 
        QueryOptions? queryOptions,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        queryOptions ??= QueryOptions.Default;
        
        var baseQuery = _dbSet.AsQueryable();
        if (queryOptions.IncludeSoftDeleted)
        {
            baseQuery = baseQuery.IgnoreQueryFilters();
        }
        
        var query = SpecificationEvaluator.GetQuery(baseQuery, spec);
        // Count operations don't need tracking, always use AsNoTracking
        query = query.AsNoTracking();
        return await query.CountAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        Guid id, 
        QueryOptions? queryOptions,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        queryOptions ??= QueryOptions.Default;
        
        var baseQuery = _dbSet.AsQueryable();
        
        if (queryOptions.IncludeSoftDeleted)
        {
            baseQuery = baseQuery.IgnoreQueryFilters();
        }
        
        var query = baseQuery.Where(e => e.Id == id);
        
        // Use AsNoTracking for existence checks as we don't need to track
        query = query.AsNoTracking();
        
        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> AnyAsync(
        ISpecification<TEntity> spec, 
        QueryOptions? queryOptions,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        queryOptions ??= QueryOptions.Default;
        
        var baseQuery = _dbSet.AsQueryable();
        if (queryOptions.IncludeSoftDeleted)
        {
            baseQuery = baseQuery.IgnoreQueryFilters();
        }
        
        var query = SpecificationEvaluator.GetQuery(baseQuery, spec);
        // Any operations don't need tracking, always use AsNoTracking
        query = query.AsNoTracking();
        
        return await query.AnyAsync(cancellationToken);
    }

    private static IQueryable<TEntity> ApplyQueryOptions(
        IQueryable<TEntity> query, 
        QueryOptions queryOptions,
        ISpecification<TEntity>? specification = null)
    {
        if (queryOptions.IncludeSoftDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        query = ApplyTracking(query, queryOptions, specification);

        return query;
    }

    /// <summary>
    /// Applies tracking settings to a query based on QueryOptions and Specification preferences.
    /// </summary>
    /// <param name="query">The query to apply tracking to.</param>
    /// <param name="queryOptions">The query options that may specify tracking preference.</param>
    /// <param name="specification">The specification that may specify tracking preference (optional).</param>
    /// <returns>The query with tracking applied.</returns>
    /// <remarks>
    /// Tracking is determined by the following priority:
    /// <list type="number">
    /// <item><description>If <paramref name="queryOptions"/> has <see cref="QueryOptions.TrackChanges"/> set to <c>true</c> (e.g., <see cref="QueryOptions.Tracking"/> or <see cref="QueryOptions.ForRestore"/>), always use tracking</description></item>
    /// <item><description>If <paramref name="queryOptions"/> matches <see cref="QueryOptions.Default"/> (by value), and <paramref name="specification"/> is provided, use the specification's preference (via <see cref="ISpecification{TEntity}.AsNoTracking"/>)</description></item>
    /// <item><description>Otherwise, use the <paramref name="queryOptions"/> preference (defaults to no tracking for better read-only performance)</description></item>
    /// </list>
    /// 
    /// This ensures that:
    /// <list type="bullet">
    /// <item><description>Explicit tracking requests (via <see cref="QueryOptions.Tracking"/>) always enable tracking</description></item>
    /// <item><description>Specifications can express their tracking intent when using <see cref="QueryOptions.Default"/> (e.g., via <see cref="BaseSpecification{TEntity}.ApplyTracking"/>)</description></item>
    /// <item><description>Read-only queries default to no tracking for better performance</description></item>
    /// </list>
    /// </remarks>
    private static IQueryable<TEntity> ApplyTracking<TEntity>(
        IQueryable<TEntity> query,
        QueryOptions queryOptions,
        ISpecification<TEntity>? specification = null)
        where TEntity : class
    {
        // Priority 1: If QueryOptions explicitly requests tracking (TrackChanges = true), always use it
        if (queryOptions.TrackChanges)
        {
            return query.AsTracking();
        }
        
        // Priority 2: If QueryOptions is Default (matches default values) and specification is provided, use specification preference
        // Records use value equality, so comparing with Default checks if values match
        var isDefaultQueryOptions = queryOptions == QueryOptions.Default;
        
        if (isDefaultQueryOptions && specification != null)
        {
            // Use specification's preference when QueryOptions is Default
            return specification.AsNoTracking 
                ? query.AsNoTracking() 
                : query.AsTracking();
        }
        
        // Priority 3: Use QueryOptions preference (which is false/AsNoTracking for Default)
        return query.AsNoTracking();
    }
    
}