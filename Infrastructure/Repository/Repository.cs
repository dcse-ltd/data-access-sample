using Infrastructure.Context;
using Infrastructure.Entity.Interfaces;
using Infrastructure.Exceptions;
using Infrastructure.Repository.Interfaces;
using Infrastructure.Repository.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository;

public class Repository<TEntity>(
    BaseAppDbContext context
    ) : IRepository<TEntity>
    where TEntity : class, IEntity
{
    private readonly DbSet<TEntity> _dbSet = context.Set<TEntity>();

    public async Task<TEntity?> GetByIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
        => await GetByIdAsync(id, QueryOptions.Default, cancellationToken);

    public async Task<TEntity?> GetByIdAsync(
        Guid id, 
        QueryOptions? queryOptions, 
        CancellationToken cancellationToken = default)
    {
        queryOptions ??= QueryOptions.Default;
        
        var query = ApplyQueryOptions(_dbSet.AsQueryable(), queryOptions);
        return await query.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }
    
    public async Task<TEntity> GetByIdOrThrowAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
        => await GetByIdOrThrowAsync(id, QueryOptions.Default, cancellationToken);
    
    public async Task<TEntity> GetByIdOrThrowAsync(
        Guid id, 
        QueryOptions? queryOptions,
        CancellationToken cancellationToken = default)
    {
        queryOptions ??= QueryOptions.Default;
        
        var query = ApplyQueryOptions(_dbSet.AsQueryable(), queryOptions);
        var entity = await query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        
        return entity ?? throw new EntityNotFoundException(typeof(TEntity).Name, id);
    }
    
    public async Task<IEnumerable<TEntity>> GetAllAsync(
        CancellationToken cancellationToken = default)
        => await GetAllAsync(QueryOptions.Default, cancellationToken);

    public async Task<IEnumerable<TEntity>> GetAllAsync(
        QueryOptions? queryOptions, 
        CancellationToken cancellationToken = default)
    {
        queryOptions ??= QueryOptions.Default;
        
        var query = ApplyQueryOptions(_dbSet.AsQueryable(), queryOptions);
        return await query.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        TEntity entity,
        CancellationToken cancellationToken)
    => await _dbSet.AddAsync(entity, cancellationToken);

    public void Update(
        TEntity entity)
    {   
        _dbSet.Update(entity);
    }

    public void Remove(TEntity entity)
    => _dbSet.Remove(entity);
    
    private static IQueryable<TEntity> ApplyQueryOptions(
        IQueryable<TEntity> query, 
        QueryOptions queryOptions)
    {
        if (queryOptions.IncludeSoftDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        if (!queryOptions.TrackChanges)
        {
            query = query.AsNoTracking();
        }

        return query;
    }
}