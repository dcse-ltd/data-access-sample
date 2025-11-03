using Infrastructure.Entity.Behaviors;
using Infrastructure.Entity.Interfaces;

namespace Infrastructure.Tests.Helpers;

public class TestLockableEntity : IEntity, ILockableEntity<TestLockableEntity>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public LockingBehavior<TestLockableEntity> Locking { get; } = new();
}

