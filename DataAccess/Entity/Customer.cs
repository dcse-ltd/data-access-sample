using DataAccess.Entity.Interfaces;
using DataAccess.Entity.Models;

namespace DataAccess.Entity;

public class Customer : IEntity, ILockableEntity, IAuditableEntity
{
    public Guid Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    
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
    
    public ICollection<Order> Orders { get; set; }
}