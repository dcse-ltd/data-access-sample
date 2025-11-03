using Infrastructure.Entity.Behaviors;
using Infrastructure.Entity.Interfaces;

namespace Infrastructure.Tests.Helpers;

public class TestAuditableEntity : IEntity, IAuditableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public AuditingBehavior Auditing { get; } = new();
}

