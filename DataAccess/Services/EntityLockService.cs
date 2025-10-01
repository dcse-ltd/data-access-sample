using DataAccess.Entity.Interfaces;
using DataAccess.Services.Interfaces;

namespace DataAccess.Services;

public class EntityLockService : IEntityLockService
{
    public void LockIfSupported<TEntity>(TEntity entity, Guid userId)
    {
        if (entity is ILockableEntity lockable)
            lockable.Lock(userId);
    }
}