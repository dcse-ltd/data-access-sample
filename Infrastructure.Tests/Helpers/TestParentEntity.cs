using Infrastructure.Entity.Attributes;
using Infrastructure.Entity.Behaviors;
using Infrastructure.Entity.Interfaces;

namespace Infrastructure.Tests.Helpers;

public class TestParentEntity : IEntity, ILockableEntity<TestParentEntity>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public LockingBehavior<TestParentEntity> Locking { get; } = new();
    
    [CascadeLock]
    public List<TestLockableEntity> Children { get; set; } = new();
}

