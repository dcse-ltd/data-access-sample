using DataAccess.Entity.Behaviors;

namespace DataAccess.Entity.Interfaces;

public interface ILockableEntity<TEntity>
where TEntity : IEntity
{
    LockingBehavior<TEntity> Locking { get; }
}