namespace Infrastructure.Exceptions;

public class EntityNotFoundException : InvalidOperationException
{
    public string EntityType { get; }
    public Guid EntityId { get; }

    public EntityNotFoundException(string entityType, Guid entityId)
        : base($"Entity of type {entityType} with ID {entityId} was not found.")
    {
        EntityType = entityType;
        EntityId = entityId;
    }

    public EntityNotFoundException(string entityType, Guid entityId, Exception innerException)
        : base($"Entity of type {entityType} with ID {entityId} was not found.", innerException)
    {
        EntityType = entityType;
        EntityId = entityId;
    }
}