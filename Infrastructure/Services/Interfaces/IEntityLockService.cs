using Infrastructure.Entity.Interfaces;

namespace Infrastructure.Services.Interfaces;

public interface IEntityLockService<in TEntity> 
    where TEntity : class, IEntity
{
    void LockIfSupported(TEntity entity, Guid userId);
    void LockWithChildrenIfSupported(TEntity entity, Guid userId, int maxDepth = 1);
    
    bool UnlockIfSupported(TEntity entity, Guid userId);
    bool UnlockWithChildrenIfSupported(TEntity entity, Guid userId, int maxDepth = 1);
    
    void ValidateLockForUpdate(TEntity entity, Guid userId);
    void ValidateLockForUpdateWithChildren(TEntity entity, Guid userId, int maxDepth = 1);
    
    void RefreshLockIfOwned(TEntity entity, Guid userId);
    void RefreshLockWithChildrenIfOwned(TEntity entity, Guid userId, int maxDepth = 1);
    
    bool IsLockedByAnotherUser(TEntity entity, Guid userId);
    
    void ForceUnlockIfSupported(TEntity entity);
    void ForceUnlockWithChildrenIfSupported(TEntity entity, int maxDepth = 1);
}