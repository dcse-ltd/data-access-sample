namespace Infrastructure.Entity.Models;

public class AuditInfo
{
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public Guid ModifiedByUserId { get; set; }
    public DateTime ModifiedAtUtc { get; set; }
}