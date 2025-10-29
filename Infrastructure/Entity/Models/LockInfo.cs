namespace Infrastructure.Entity.Models;

public class LockInfo
{
    public Guid? LockedByUserId { get; set; }
    public DateTime? LockedAtUtc { get; set; }
    public int LockTimeoutMinutes { get; set; } = 15;

    public bool IsExpired()
    {
        if (LockedByUserId == null || LockedAtUtc == null)
            return false;
        
        var expirationTime = LockedAtUtc.Value.AddMinutes(LockTimeoutMinutes);
        return DateTime.UtcNow > expirationTime;
    }
    
    public bool IsLockedBy(Guid userId)
    {
        return LockedByUserId == userId && !IsExpired();
    }
    
    public bool IsLocked()
    {
        return LockedByUserId != null && !IsExpired();
    }
}