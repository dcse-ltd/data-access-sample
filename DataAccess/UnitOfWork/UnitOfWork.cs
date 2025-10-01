using DataAccess.Context;
using DataAccess.UnitOfWork.Interfaces;

namespace DataAccess.UnitOfWork;

public class UnitOfWork(
    AppDbContext context,
    IEnumerable<IUnitOfWorkProcessor> processors) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var processor in processors)
            await processor.BeforeSaveChangesAsync(context);

        var result = await context.SaveChangesAsync(cancellationToken);

        foreach (var processor in processors)
            await processor.AfterSaveChangesAsync(context);

        return result;
    }
    
    public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => context.Database.BeginTransactionAsync(cancellationToken);
    public Task CommitTransactionAsync(CancellationToken cancellationToken = default) => context.Database.CommitTransactionAsync(cancellationToken);
    public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) => context.Database.RollbackTransactionAsync(cancellationToken);
    public void Dispose() => context.Dispose();
}