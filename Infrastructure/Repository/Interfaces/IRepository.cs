using Infrastructure.Entity.Interfaces;
using Infrastructure.Repository.Models;

namespace Infrastructure.Repository.Interfaces;

public interface IRepository<TEntity> where TEntity : IEntity
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
    
    Task<IEnumerable<TEntity>> GetAllAsync(
        CancellationToken cancellationToken);
    
    Task<IEnumerable<TEntity>> GetAllAsync(
        QueryOptions? queryOptions,
        CancellationToken cancellationToken);
    
    Task AddAsync(
        TEntity entity, 
        CancellationToken cancellationToken);
    
    void Update(
        TEntity entity);
    
    void Remove(
        TEntity entity);
}