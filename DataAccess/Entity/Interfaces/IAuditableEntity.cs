using DataAccess.Entity.Models;

namespace DataAccess.Entity.Interfaces;

public interface IAuditableEntity
{
    public AuditInfo AuditInfo { get; set; }
}