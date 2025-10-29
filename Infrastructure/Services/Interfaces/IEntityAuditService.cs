using Infrastructure.Entity.Interfaces;

namespace Infrastructure.Services.Interfaces;

public interface IEntityAuditService<in TEntity> 
    where TEntity : class, IEntity
{
    void StampForCreate(TEntity entity, Guid userId);
    void StampForUpdate(TEntity entity, Guid userId);
}