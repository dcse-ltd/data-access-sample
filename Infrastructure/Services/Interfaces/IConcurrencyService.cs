using Infrastructure.Entity.Interfaces;
using Infrastructure.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Service contract for handling concurrency conflicts when updating entities.
/// Converts EF Core <see cref="DbUpdateConcurrencyException"/> into domain-specific <see cref="ConcurrencyException"/>.
/// </summary>
/// <typeparam name="TEntity">The entity type that implements <see cref="IEntity"/> and optionally <see cref="IConcurrencyEntity"/>.</typeparam>
/// <remarks>
/// Implementations of this interface handle concurrency conflicts by extracting row version information
/// from both the client entity and the database, allowing the application to make informed decisions
/// about how to resolve conflicts.
/// </remarks>
public interface IConcurrencyService<in TEntity>
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
    ConcurrencyException HandleConcurrencyException(
        TEntity entity,
        DbUpdateConcurrencyException exception);
}