using DataAccess.Context;
using DataAccess.Entity.Interfaces;
using DataAccess.Exceptions;
using DataAccess.UnitOfWork.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DataAccess.UnitOfWork.Processors;

public class ConcurrencyConflictProcessor(ILogger<ConcurrencyConflictProcessor> logger) : IUnitOfWorkProcessor
{
    public Task BeforeSaveChangesAsync(BaseAppDbContext context) 
        => Task.CompletedTask;

    public Task AfterSaveChangesAsync(BaseAppDbContext context) 
        => Task.CompletedTask;

    public ConcurrencyException HandleConcurrencyException(
        DbUpdateConcurrencyException exception)
    {
        var entry = exception.Entries.FirstOrDefault();
        if (entry == null)
        {
            return new ConcurrencyException("Unknown", Guid.Empty, null, null);
        }

        var entityType = entry.Entity.GetType().Name;
        var entityId = entry.Entity is IEntity entity ? entity.Id : Guid.Empty;

        byte[]? clientRowVersion = null;
        if (entry.Entity is IConcurrencyEntity concurrent)
        {
            clientRowVersion = concurrent.Concurrency.RowVersion;
        }

        byte[]? databaseRowVersion = null;
        var databaseValues = entry.GetDatabaseValues();
        var concurrencyEntry = databaseValues?.Properties
            .FirstOrDefault(p => p.Name == "Concurrency");
            
        if (concurrencyEntry != null)
        {
            var concurrencyValues = databaseValues?.GetValue<object>("Concurrency");
            if (concurrencyValues != null)
            {
                var rowVersionProp = concurrencyValues.GetType().GetProperty("RowVersion");
                databaseRowVersion = rowVersionProp?.GetValue(concurrencyValues) as byte[];
            }
        }

        logger.LogWarning(
            "Concurrency conflict detected for {EntityType} {EntityId}",
            entityType,
            entityId);

        return new ConcurrencyException(
            entityType,
            entityId,
            clientRowVersion,
            databaseRowVersion);
    }
}