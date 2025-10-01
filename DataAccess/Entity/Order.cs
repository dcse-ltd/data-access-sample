using DataAccess.Entity.Interfaces;
using DataAccess.Entity.Models;

namespace DataAccess.Entity;

public class Order : IEntity, ILockableEntity, IAuditableEntity
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; }
    
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