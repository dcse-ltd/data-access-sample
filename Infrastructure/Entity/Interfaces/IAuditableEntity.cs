using Infrastructure.Entity.Behaviors;

namespace Infrastructure.Entity.Interfaces;

public interface IAuditableEntity
{
    public AuditingBehavior Auditing { get; }
}