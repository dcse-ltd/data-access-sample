using DataAccess.Context;
using DataAccess.Entity.Interfaces;
using DataAccess.Repository.Interfaces;
using DataAccess.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repository;

public class Repository<T>(
    AppDbContext context,
    IEntityLockService entityLockService,
    ICurrentUserService currentUserService) : IRepository<T>
    where T : class, IEntity
{
    private readonly DbSet<T> _dbSet = context.Set<T>();

    public async Task<T?> GetByIdAsync(Guid id, bool lockForEdit = false, CancellationToken cancellationToken = default)
    {
        var entity = await _dbSet.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        var userId = currentUserService.UserId;
        if (lockForEdit)
            entityLockService.LockIfSupported(entity, userId);
        
        return entity;
    }
    
    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken) 
        => await _dbSet.ToListAsync(cancellationToken);
    
    public async Task AddAsync(T entity, CancellationToken cancellationToken) 
        => await _dbSet.AddAsync(entity, cancellationToken);
    
    public void Update(T entity)
        => Task.FromResult(_dbSet.Update(entity));
    
    public void Remove(T entity) 
        => _dbSet.Remove(entity);
}