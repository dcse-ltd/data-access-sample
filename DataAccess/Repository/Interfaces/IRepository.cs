using DataAccess.Entity.Interfaces;

namespace DataAccess.Repository.Interfaces;

public interface IRepository<TEntity> where TEntity : IEntity
{
    Task<TEntity> GetByIdAsync(
        Guid id, 
        bool trackChanges = true,
        CancellationToken cancellationToken = default);
    
    Task<IEnumerable<TEntity>> GetAllAsync(
        CancellationToken cancellationToken);
    
    Task AddAsync(
        TEntity entity, 
        CancellationToken cancellationToken);
    
    TEntity UpdateAsync(
        TEntity entity);
    
    void Remove(
        TEntity entity);
}