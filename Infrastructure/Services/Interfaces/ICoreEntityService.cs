using Infrastructure.Entity.Interfaces;
using Infrastructure.Repository.Models;
using Infrastructure.Services.Models;

namespace Infrastructure.Services.Interfaces;

public interface ICoreEntityService<TEntity> where TEntity : class, IEntity
{
    Task<TEntity?> GetByIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default);
    
    Task<TEntity?> GetByIdAsync(
        Guid id, 
        QueryOptions? queryOptions, 
        CancellationToken cancellationToken = default);
    
    Task<TEntity> GetByIdOrThrowAsync(
        Guid id, 
        CancellationToken cancellationToken = default);
    
    Task<TEntity> GetByIdOrThrowAsync(
        Guid id, 
        QueryOptions? queryOptions, 
        CancellationToken cancellationToken = default);
    
    Task<TEntity> GetByIdWithLockAsync(
        Guid id, 
        CancellationToken cancellationToken = default);
    
    Task<TEntity> GetByIdWithLockAsync(
        Guid id, 
        LockOptions? lockOptions, 
        CancellationToken cancellationToken = default);
    
    Task RefreshLockAsync(
        Guid id, 
        CancellationToken cancellationToken = default);
    
    Task<IEnumerable<TEntity>> GetAllAsync(
        CancellationToken cancellationToken = default);
    
    Task<IEnumerable<TEntity>> GetAllAsync(
        QueryOptions? queryOptions,
        CancellationToken cancellationToken = default);
    
    Task<TEntity> CreateAsync(
        TEntity entity, 
        CancellationToken cancellationToken = default);
    
    Task<TEntity> UpdateAsync(
        Guid id, 
        Action<TEntity> updateAction, 
        CancellationToken cancellationToken = default);
    
    Task<TEntity> UpdateAsync(
        Guid id, 
        Action<TEntity> updateAction, 
        LockOptions? lockOptions, 
        CancellationToken cancellationToken = default);
    
    Task DeleteAsync(
        Guid id, 
        CancellationToken cancellationToken = default);
    
    Task DeleteAsync(
        Guid id, 
        LockOptions? lockOptions, 
        CancellationToken cancellationToken = default);
    
    Task HardDeleteAsync(
        Guid id, 
        CancellationToken cancellationToken = default);
    
    Task HardDeleteAsync(
        Guid id, 
        LockOptions? lockOptions, 
        CancellationToken cancellationToken = default);
    
    Task RestoreAsync(
        Guid id, 
        bool includeChildren, 
        CancellationToken cancellationToken = default);
}