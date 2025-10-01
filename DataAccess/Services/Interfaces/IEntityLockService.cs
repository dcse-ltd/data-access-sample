namespace DataAccess.Services.Interfaces;

public interface IEntityLockService
{
    void LockIfSupported<TEntity>(TEntity entity, Guid userId);
}