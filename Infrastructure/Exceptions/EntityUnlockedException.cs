namespace Infrastructure.Exceptions;

public class EntityUnlockedException(string entityType, Guid entityId)
    : InvalidOperationException(
        $"{entityType} entity with the Id: {entityId} has not been locked prior to update")
{
    public string EntityType { get; } = entityType;
    public Guid EntityId { get; } = entityId;
}