namespace Infrastructure.Entity.Models;

public class SoftDeleteInfo
{
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public Guid? DeletedByUserId { get; set; }
    
    public bool IsDeletedBy(Guid userId)
    {
        return IsDeleted && DeletedByUserId == userId;
    }
    
    public bool IsDeletedLongerThan(TimeSpan duration)
    {
        return IsDeleted && 
               DeletedAtUtc.HasValue && 
               DateTime.UtcNow - DeletedAtUtc.Value > duration;
    }
}