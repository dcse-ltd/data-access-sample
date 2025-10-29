namespace Infrastructure.Exceptions;

public class EntityLockedException(string entityType, Guid lockedByUserId, DateTime? lockedAtUtc)
    : InvalidOperationException(
        $"{entityType} is currently locked by user {lockedByUserId} since {lockedAtUtc?.ToString("yyyy-MM-dd HH:mm:ss") ?? "unknown time"}")
{
    public string EntityType { get; } = entityType;
    public Guid LockedByUserId { get; } = lockedByUserId;
    public DateTime? LockedAtUtc { get; } = lockedAtUtc;
}