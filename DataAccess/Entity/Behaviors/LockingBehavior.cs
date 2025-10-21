using DataAccess.Entity.Interfaces;
using DataAccess.Entity.Models;
using DataAccess.Exceptions;

namespace DataAccess.Entity.Behaviors;

public class LockingBehavior<TEntity>
    where TEntity : IEntity 
{
    private readonly object _syncRoot = new();
    private LockInfo _lockInfo = new();

    public LockInfo LockInfo
    {
        get
        {
            lock (_syncRoot)
            {
                return new LockInfo
                {
                    LockedByUserId = _lockInfo.LockedByUserId,
                    LockedAtUtc = _lockInfo.LockedAtUtc
                };
            }
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            lock (_syncRoot)
            {
                _lockInfo = value;
            }
        }
    }

    public void Lock(Guid userId)
    {
        lock (_syncRoot)
        {
            if (_lockInfo.IsLockedBy(userId))
            {
                _lockInfo.LockedAtUtc = DateTime.UtcNow;
                return;
            }

            if (_lockInfo.IsExpired())
            {
                _lockInfo.LockedByUserId = userId;
                _lockInfo.LockedAtUtc = DateTime.UtcNow;
                return;
            }

            if (_lockInfo.IsLocked())
            {
                throw new EntityLockedException(
                    typeof(TEntity).Name,
                    _lockInfo.LockedByUserId!.Value,
                    _lockInfo.LockedAtUtc);
            }

            _lockInfo.LockedByUserId = userId;
            _lockInfo.LockedAtUtc = DateTime.UtcNow;
        }
    }
    
    public bool Unlock(Guid userId)
    {
        lock (_syncRoot)
        {
            if (_lockInfo.LockedByUserId == null)
                return true;

            if (!_lockInfo.IsLockedBy(userId) && !_lockInfo.IsExpired())
                return false;

            _lockInfo.LockedByUserId = null;
            _lockInfo.LockedAtUtc = null;
            return true;
        }
    }

    public void UnlockOrThrow(Guid userId)
    {
        lock (_syncRoot)
        {
            if (!_lockInfo.IsLocked())
                return;

            if (!_lockInfo.IsLockedBy(userId) && !_lockInfo.IsExpired())
                throw new EntityLockedException(
                    typeof(TEntity).Name,
                    _lockInfo.LockedByUserId!.Value,
                    _lockInfo.LockedAtUtc);

            _lockInfo.LockedByUserId = null;
            _lockInfo.LockedAtUtc = null;
        }
    }
    
    public void ForceUnlock()
    {
        lock (_syncRoot)
        {
            _lockInfo.LockedByUserId = null;
            _lockInfo.LockedAtUtc = null;
        }
    }

    public void RefreshLock(Guid userId)
    {
        lock (_syncRoot)
        {
            if (_lockInfo.IsLockedBy(userId))
            {
                _lockInfo.LockedAtUtc = DateTime.UtcNow;
            }
        }
    }
}