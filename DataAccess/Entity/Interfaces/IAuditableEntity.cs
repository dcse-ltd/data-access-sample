using DataAccess.Entity.Behaviors;

namespace DataAccess.Entity.Interfaces;

public interface IAuditableEntity
{
    public AuditingBehavior Auditing { get; }
}