using DataAccess.Context;
using DataAccess.UnitOfWork.Interfaces;
using DataAccess.UnitOfWork.Processors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace DataAccess.UnitOfWork;

public class UnitOfWork(
    BaseAppDbContext context,
    IEnumerable<IUnitOfWorkProcessor> processors,
    ConcurrencyConflictProcessor concurrencyProcessor,
    ILogger logger) : IUnitOfWork
{
    private IDbContextTransaction? _currentTransaction;
    
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var processor in processors)
            await processor.BeforeSaveChangesAsync(context);

        try
        {
            var result = await context.SaveChangesAsync(cancellationToken);

            if (HasActiveTransaction) 
                return result;
            
            foreach (var processor in processors)
                await processor.AfterSaveChangesAsync(context);

            return result;
        }
        catch (DbUpdateConcurrencyException e)
        {
            throw concurrencyProcessor.HandleConcurrencyException(e);
        }
    }
    
    public async Task BeginTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            throw new InvalidOperationException(
                "A transaction is already in progress. Nested transactions are not supported.");
        }
        
        _currentTransaction = await context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
        {
            throw new InvalidOperationException("No transaction is currently in progress.");
        }

        try
        {
            logger.LogDebug("Committing transaction");
            
            await _currentTransaction.CommitAsync(cancellationToken);

            foreach (var processor in processors)
            {
                await processor.AfterSaveChangesAsync(context);
            }

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

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
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
}