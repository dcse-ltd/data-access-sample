using Infrastructure.Entity.Interfaces;
using Infrastructure.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class EntityAuditService<TEntity>(
    ILogger<EntityAuditService<TEntity>> logger
) : IEntityAuditService<TEntity>
    where TEntity : class, IEntity
{
    public void StampForCreate(TEntity entity, Guid userId)
    {
        if (entity is not IAuditableEntity auditable) 
            return;
        
        var now = DateTime.UtcNow;
        logger.LogInformation("Stamping {Entity} with the Id: {EntityId} with a created by date of {AuditDate} and created by User with the ID {UserId}", typeof(TEntity).Name, entity.Id, now, userId);
        auditable.Auditing.MarkCreated(userId);
        auditable.Auditing.MarkModified(userId);
    }

    public void StampForUpdate(TEntity entity, Guid userId)
    {
        if (entity is not IAuditableEntity auditable) 
            return;
        
        var now = DateTime.UtcNow;
        logger.LogInformation("Stamping {Entity} with the Id: {EntityId} with an updated by date of {AuditDate} and updated by User with the ID {UserId}", typeof(TEntity).Name, entity.Id, now, userId);
        auditable.Auditing.MarkModified(userId);
    }
}