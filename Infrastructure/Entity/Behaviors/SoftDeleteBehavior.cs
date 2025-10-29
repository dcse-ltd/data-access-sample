using Infrastructure.Entity.Models;

namespace Infrastructure.Entity.Behaviors;

public class SoftDeleteBehavior
{
    private readonly object _syncRoot = new();
    private SoftDeleteInfo _softDeleteInfo = new();
    
    public SoftDeleteInfo SoftDeleteInfo
    {
        get
        {
            lock (_syncRoot)
            {
                return new SoftDeleteInfo
                {
                    IsDeleted = _softDeleteInfo.IsDeleted,
                    DeletedAtUtc = _softDeleteInfo.DeletedAtUtc,
                    DeletedByUserId = _softDeleteInfo.DeletedByUserId
                };
            }
        }
        private set
        {
            ArgumentNullException.ThrowIfNull(value);
            lock (_syncRoot)
            {
                _softDeleteInfo = value;
            }
        }
    }

    public void MarkSoftDeleted(Guid userId)
    {
        lock (_syncRoot)
        {
            _softDeleteInfo.IsDeleted = true;
            _softDeleteInfo.DeletedByUserId = userId;
            _softDeleteInfo.DeletedAtUtc = DateTime.UtcNow;
        }
    }

    public void Restore()
    {
        lock (_syncRoot)
        {
            _softDeleteInfo.IsDeleted = false;
            _softDeleteInfo.DeletedByUserId = null;
            _softDeleteInfo.DeletedAtUtc = null;      
        }
    }
}