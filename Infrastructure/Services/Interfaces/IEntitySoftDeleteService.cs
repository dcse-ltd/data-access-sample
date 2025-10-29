using Infrastructure.Entity.Interfaces;

namespace Infrastructure.Services.Interfaces;

public interface IEntitySoftDeleteService<in TEntity> 
    where TEntity : class, IEntity
{
    void StampForDelete(
        TEntity entity, 
        Guid userId);
    
    void StampForDeleteWithChildren(
        TEntity entity, 
        Guid userId, 
        int maxDepth = 1);
    
    void StampForRestore(
        TEntity entity);
    
    void StampForRestoreWithChildren(
        TEntity entity, 
        int maxDepth = 1);
}