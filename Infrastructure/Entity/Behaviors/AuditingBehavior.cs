using Infrastructure.Entity.Models;

namespace Infrastructure.Entity.Behaviors;

public class AuditingBehavior
{
    private AuditInfo _auditInfo = new();

    public AuditInfo AuditInfo
    {
        get => _auditInfo;
        set => _auditInfo = value;
    }

    public void MarkCreated(Guid userId)
    {
        _auditInfo.CreatedByUserId = userId;
        _auditInfo.CreatedAtUtc = DateTime.UtcNow;
    }

    public void MarkModified(Guid userId)
    {
        _auditInfo.ModifiedByUserId = userId;
        _auditInfo.ModifiedAtUtc = DateTime.UtcNow;
    }
}