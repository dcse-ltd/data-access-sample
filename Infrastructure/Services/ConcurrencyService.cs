using Infrastructure.Entity.Interfaces;
using Infrastructure.Exceptions;
using Infrastructure.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class ConcurrencyService<TEntity>(
    ILogger<ConcurrencyService<TEntity>> logger
    ) : IConcurrencyService<TEntity>
    where TEntity : class, IEntity
{
    public ConcurrencyException HandleConcurrencyException(
        TEntity entity,
        DbUpdateConcurrencyException exception)
    {
        if (entity is not IConcurrencyEntity concurrencyEntity)
            throw exception;

        var entry = exception.Entries.FirstOrDefault();
        if (entry == null)
            throw exception;

        var databaseValues = entry.GetDatabaseValues();
        if (databaseValues == null)
        {
            throw new EntityNotFoundException(typeof(TEntity).Name, entity.Id);
        }

        var clientRowVersion = concurrencyEntity.Concurrency.RowVersion;
        var databaseRowVersion = databaseValues.GetValue<byte[]>(nameof(IConcurrencyEntity.Concurrency));

        logger.LogWarning(
            "Concurrency conflict detected for {EntityType} {EntityId}",
            typeof(TEntity).Name,
            entity.Id);

        return new ConcurrencyException(
            typeof(TEntity).Name,
            entity.Id,
            clientRowVersion,
            databaseRowVersion);
    }
}