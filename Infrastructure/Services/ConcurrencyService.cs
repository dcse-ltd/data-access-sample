using System.Reflection;
using Infrastructure.Entity.Interfaces;
using Infrastructure.Exceptions;
using Infrastructure.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Service for handling concurrency conflicts when updating entities.
/// Provides exception handling for <see cref="DbUpdateConcurrencyException"/> 
/// and converts them into domain-specific <see cref="ConcurrencyException"/>.
/// </summary>
/// <typeparam name="TEntity">The entity type that implements <see cref="IEntity"/> and optionally <see cref="IConcurrencyEntity"/>.</typeparam>
/// <remarks>
/// This service is typically used internally by <see cref="CoreEntityService{TEntity}"/> 
/// to handle concurrency conflicts during update operations. When a concurrency conflict occurs,
/// it extracts the client and database row versions and creates a <see cref="ConcurrencyException"/>
/// that can be handled by the application layer.
/// 
/// Only entities implementing <see cref="IConcurrencyEntity"/> will have their concurrency 
/// information extracted; otherwise, the original exception is re-thrown.
/// </remarks>
public class ConcurrencyService<TEntity>(
    ILogger<ConcurrencyService<TEntity>> logger
    ) : IConcurrencyService<TEntity>
    where TEntity : class, IEntity
{
    /// <summary>
    /// Handles a <see cref="DbUpdateConcurrencyException"/> by extracting concurrency information
    /// and converting it into a domain-specific <see cref="ConcurrencyException"/>.
    /// </summary>
    /// <param name="entity">The entity that caused the concurrency conflict.</param>
    /// <param name="exception">The EF Core concurrency exception.</param>
    /// <returns>A <see cref="ConcurrencyException"/> containing client and database row versions.</returns>
    /// <exception cref="DbUpdateConcurrencyException">
    /// Thrown if the entity doesn't implement <see cref="IConcurrencyEntity"/> 
    /// or if the exception cannot be processed.
    /// </exception>
    /// <exception cref="EntityNotFoundException">
    /// Thrown if the entity was deleted in the database during the update operation.
    /// </exception>
    /// <remarks>
    /// This method extracts the row version from both the client entity and the database,
    /// allowing the application to determine what changed and potentially merge changes.
    /// The exception contains both versions so the client can make informed decisions.
    /// </remarks>
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
        
        // Get the property name from IConcurrencyEntity interface
        var concurrencyProperty = typeof(IConcurrencyEntity).GetProperty("Concurrency");
        if (concurrencyProperty == null)
        {
            logger.LogError(
                "Concurrency property not found on IConcurrencyEntity interface for {EntityType} {EntityId}",
                typeof(TEntity).Name,
                entity.Id);
            throw exception;
        }
        
        var databaseRowVersion = databaseValues.GetValue<byte[]>(concurrencyProperty.Name);

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