using Infrastructure.Context;
using Infrastructure.UnitOfWork.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Infrastructure.UnitOfWork;

public class UnitOfWork(
    BaseAppDbContext context,
    ILogger<UnitOfWork> logger) : IUnitOfWork
{
    private IDbContextTransaction? _currentTransaction;
    
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await context.SaveChangesAsync(cancellationToken);
    
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