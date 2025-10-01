namespace DataAccess.Entity.Models;

public class LockInfo
{
    public Guid? LockedByUserId { get; set; }
    public DateTime? LockedAtUtc { get; set; }
}