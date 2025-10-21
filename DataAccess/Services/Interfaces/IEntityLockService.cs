using DataAccess.Entity.Interfaces;

namespace DataAccess.Services.Interfaces;

public interface IEntityLockService<in TEntity> 
    where TEntity : class, IEntity
{
    void LockIfSupported(TEntity entity, Guid userId);
    bool UnlockIfSupported(TEntity entity, Guid userId);
    void ValidateLockForUpdate(TEntity entity, Guid userId);
    void RefreshLockIfOwned(TEntity entity, Guid userId);
    bool IsLockedByAnotherUser(TEntity entity, Guid userId);
    void ForceUnlockIfSupported(TEntity entity);
}