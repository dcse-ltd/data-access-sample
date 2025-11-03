using Infrastructure.Context;
using Infrastructure.UnitOfWork.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Infrastructure.UnitOfWork;

/// <summary>
/// Unit of Work implementation for managing database transactions and change persistence.
/// Provides transactional support for operations that need to be atomic.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Transaction Pattern:</strong>
/// This implementation follows a three-step transaction pattern:
/// <list type="number">
/// <item><description><see cref="BeginTransactionAsync"/> - Starts a database transaction</description></item>
/// <item><description><see cref="SaveChangesAsync"/> - Saves changes to the database (can be called multiple times)</description></item>
/// <item><description><see cref="CommitTransactionAsync"/> - Commits the transaction (must be called after SaveChangesAsync)</description></item>
/// </list>
/// </para>
/// 
/// <para>
/// <strong>Important:</strong> <see cref="SaveChangesAsync"/> must be called before <see cref="CommitTransactionAsync"/>.
/// The commit operation does NOT automatically save changes - it only commits the transaction.
/// This design allows for flexibility where services can call SaveChangesAsync multiple times
/// within a single transaction before committing.
/// </para>
/// 
/// <para>
/// <strong>Usage Example:</strong>
/// <code>
/// await unitOfWork.BeginTransactionAsync(cancellationToken);
/// try
/// {
///     // Perform operations that modify entities
///     await repository.AddAsync(entity1, cancellationToken);
///     await unitOfWork.SaveChangesAsync(cancellationToken); // Save changes
///     
///     await repository.AddAsync(entity2, cancellationToken);
///     await unitOfWork.SaveChangesAsync(cancellationToken); // Save more changes
///     
///     await unitOfWork.CommitTransactionAsync(cancellationToken); // Commit transaction
/// }
/// catch
/// {
///     await unitOfWork.RollbackTransactionAsync(cancellationToken);
///     throw;
/// }
/// </code>
/// </para>
/// </remarks>
public class UnitOfWork(
    BaseAppDbContext context,
    ILogger<UnitOfWork> logger) : IUnitOfWork
{
    private IDbContextTransaction? _currentTransaction;
    
    /// <summary>
    /// Saves all changes made in the current context to the database.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The number of state entries written to the database.</returns>
    /// <remarks>
    /// This method can be called multiple times within a transaction. Each call will
    /// persist any pending changes to the database. Changes are not committed until
    /// <see cref="CommitTransactionAsync"/> is called.
    /// </remarks>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await context.SaveChangesAsync(cancellationToken);
    }
    
    /// <summary>
    /// Begins a new database transaction.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if a transaction is already in progress (nested transactions are not supported).
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method starts a new database transaction. All subsequent operations will be
    /// part of this transaction until it is committed or rolled back.
    /// </para>
    /// <para>
    /// After calling this method, you must call <see cref="SaveChangesAsync"/> to persist
    /// changes, and then <see cref="CommitTransactionAsync"/> to commit the transaction.
    /// </para>
    /// </remarks>
    public async Task BeginTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_currentTransaction != null)
        {
            throw new InvalidOperationException(
                "A transaction is already in progress. Nested transactions are not supported.");
        }
        
        _currentTransaction = await context.Database.BeginTransactionAsync(cancellationToken);
    }

    /// <summary>
    /// Commits the current database transaction.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if no transaction is currently in progress.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <strong>Important:</strong> This method does NOT call <see cref="SaveChangesAsync"/>.
    /// You must call <see cref="SaveChangesAsync"/> before calling this method to ensure
    /// all changes are persisted to the database before committing the transaction.
    /// </para>
    /// <para>
    /// If <see cref="SaveChangesAsync"/> has not been called, the transaction will commit
    /// but no changes will be persisted (or changes from a previous SaveChangesAsync call
    /// will be committed).
    /// </para>
    /// <para>
    /// This method will automatically roll back the transaction if an error occurs during commit.
    /// </para>
    /// </remarks>
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_currentTransaction == null)
        {
            throw new InvalidOperationException("No transaction is currently in progress.");
        }

        try
        {
            logger.LogDebug("Committing transaction");
            
            await _currentTransaction.CommitAsync(cancellationToken);

            logger.LogDebug("Transaction committed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error committing transaction, rolling back");
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    /// <summary>
    /// Rolls back the current database transaction, discarding all changes.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <remarks>
    /// This method rolls back the current transaction and discards all changes made
    /// within the transaction. If no transaction is active, this method does nothing.
    /// </remarks>
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_currentTransaction == null)
        {
            logger.LogWarning("Rollback requested but no transaction is active");
            return;
        }

        try
        {
            logger.LogWarning("Rolling back transaction");
            await _currentTransaction.RollbackAsync(cancellationToken);
            logger.LogWarning("Transaction rolled back successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error rolling back transaction");
            throw;
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }
    
    /// <summary>
    /// Gets a value indicating whether a transaction is currently active.
    /// </summary>
    /// <returns>True if a transaction is active; otherwise, false.</returns>
    public bool HasActiveTransaction => _currentTransaction != null;

    private async Task DisposeTransactionAsync()
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public void Dispose()
    {
        _currentTransaction?.Dispose();
        context.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeTransactionAsync();
        await context.DisposeAsync();
    }
}