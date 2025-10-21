using DataAccess.Context;
using DataAccess.Entity.Interfaces;
using DataAccess.Exceptions;
using DataAccess.Repository.Interfaces;
using DataAccess.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repository;

public class Repository<TEntity>(
    BaseAppDbContext context,
    IEntityLockService<TEntity> entityLockService,
    ICurrentUserService currentUserService) : IRepository<TEntity>
    where TEntity : class, IEntity
{
    private readonly DbSet<TEntity> _dbSet = context.Set<TEntity>();

    public async Task<TEntity> GetByIdAsync(
        Guid id, 
        bool trackChanges = true,
        CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? _dbSet : _dbSet.AsNoTracking();
        var entity = await query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        
        return entity ?? throw new EntityNotFoundException(typeof(TEntity).Name, id);
    }
    
    public async Task<IEnumerable<TEntity>> GetAllAsync(
        CancellationToken cancellationToken) 
        => await _dbSet.AsNoTracking().ToListAsync(cancellationToken);
    
    public async Task AddAsync(
        TEntity entity, 
        CancellationToken cancellationToken) 
        => await _dbSet.AddAsync(entity, cancellationToken);

    public TEntity UpdateAsync(
        TEntity entity)
    {   
        var userId = currentUserService.UserId;
        entityLockService.ValidateLockForUpdate(entity, userId);
        _dbSet.Update(entity);
        return entity;
    }

    public void Remove(TEntity entity)
    {
        var userId = currentUserService.UserId;
        entityLockService.ValidateLockForUpdate(entity, userId);
        _dbSet.Remove(entity);   
    }
}