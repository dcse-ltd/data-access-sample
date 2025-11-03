using Infrastructure.Entity.Attributes;
using Infrastructure.Entity.Behaviors;
using Infrastructure.Entity.Interfaces;

namespace Infrastructure.Tests.Helpers;

public class TestParentSoftDeletableEntity : IEntity, ISoftDeletableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public SoftDeleteBehavior Deleted { get; } = new();
    
    [CascadeDelete]
    public List<TestSoftDeletableEntity> Children { get; set; } = new();
}

