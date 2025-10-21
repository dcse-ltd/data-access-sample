using DataAccess.Entity.Interfaces;
using DataAccess.Services.Interfaces;
using DataAccess.UnitOfWork.Processors;

namespace DataAccess.Services;

public class EntityLockService<TEntity>(LockReleaseProcessor lockReleaseProcessor) 
    : IEntityLockService<TEntity>
    where TEntity : class, IEntity
{
    public void LockIfSupported(TEntity entity, Guid userId)
    {
        if (entity is ILockableEntity<TEntity> lockable)
        {
            lockable.Locking.Lock(userId);
        }
    }

    public bool UnlockIfSupported(TEntity entity, Guid userId)
    {
        if (entity is ILockableEntity<TEntity> lockable)
        {
            return lockable.Locking.Unlock(userId);
        }

        return true;
    }

    public void ValidateLockForUpdate(TEntity entity, Guid userId)
    {
        if (entity is not ILockableEntity<TEntity> lockable)
            return;
        
        var lockInfo = lockable.Locking.LockInfo;
        
        if (lockInfo.LockedByUserId == null)
            return;
        
        if (lockInfo.IsExpired())
        {
            lockable.Locking.ForceUnlock();
            return;
        }

        if (lockInfo.IsLockedBy(userId))
        {
            lockReleaseProcessor.Track(entity);
            return;
        }

        lockable.Locking.UnlockOrThrow(userId);
    }

    public void RefreshLockIfOwned(TEntity entity, Guid userId)
    {
        if (entity is ILockableEntity<TEntity> lockable)
        {
            lockable.Locking.RefreshLock(userId);
        }
    }

    public bool IsLockedByAnotherUser(TEntity entity, Guid userId)
    {
        if (entity is not ILockableEntity<TEntity> lockable)
            return false;
        
        var lockInfo = lockable.Locking.LockInfo;
        if (lockInfo.LockedByUserId == null || lockInfo.IsExpired())
            return false;
        
        return !lockInfo.IsLockedBy(userId);
    }
    
    public void ForceUnlockIfSupported(TEntity entity)
    {
        if (entity is ILockableEntity<TEntity> lockable)
        {
            lockable.Locking.ForceUnlock();
        }
    }
}