using DataAccess.Entity.Interfaces;
using DataAccess.Entity.Models;

namespace DataAccess.Entity;

public class Product : IEntity, ILockableEntity, IAuditableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    
    public LockInfo LockInfo { get; set; }
    public AuditInfo AuditInfo { get; set; }

    public void Lock(Guid userId)
    {
        LockInfo.LockedByUserId = userId;
        LockInfo.LockedAtUtc = DateTime.UtcNow;
    }

    public void Unlock()
    {
        LockInfo.LockedByUserId = null;
        LockInfo.LockedAtUtc = null;
    }
    
    public ICollection<OrderProduct> OrderProducts { get; set; }
}