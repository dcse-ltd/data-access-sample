using Infrastructure.Entity.Behaviors;

namespace Infrastructure.Entity.Interfaces;

public interface ILockableEntity<TEntity>
where TEntity : IEntity
{
    LockingBehavior<TEntity> Locking { get; }
}