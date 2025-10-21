namespace DataAccess.Exceptions;

public class ConcurrencyException(
    string entityType,
    Guid entityId,
    byte[]? clientRowVersion,
    byte[]? databaseRowVersion)
    : InvalidOperationException(
        $"The {entityType} with ID {entityId} has been modified by another user. Please refresh and try again.")
{
    public string EntityType { get; } = entityType;
    public Guid EntityId { get; } = entityId;
    public byte[]? ClientRowVersion { get; } = clientRowVersion;
    public byte[]? DatabaseRowVersion { get; } = databaseRowVersion;
}