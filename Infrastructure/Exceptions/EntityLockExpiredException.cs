namespace Infrastructure.Exceptions;

public class EntityLockExpiredException(
    string entityType,
    Guid entityId,
    Guid lockedByUserId,
    DateTime lockedAtUtc,
    int lockTimeoutMinutes)
    : InvalidOperationException($"{entityType} with ID {entityId} had its lock expire. " +
                                $"It was locked by user {lockedByUserId} at {lockedAtUtc:yyyy-MM-dd HH:mm:ss} UTC " +
                                $"and expired after {lockTimeoutMinutes} minutes.")
{
    public string EntityType { get; } = entityType;
    public Guid EntityId { get; } = entityId;
    public Guid LockedByUserId { get; } = lockedByUserId;
    public DateTime LockedAtUtc { get; } = lockedAtUtc;
    public DateTime ExpiredAtUtc { get; } = lockedAtUtc.AddMinutes(lockTimeoutMinutes);
    public int LockTimeoutMinutes { get; } = lockTimeoutMinutes;
}