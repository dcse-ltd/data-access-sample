using DataAccess.Context;
using DataAccess.Entity.Interfaces;
using DataAccess.UnitOfWork.Interfaces;

namespace DataAccess.UnitOfWork.Processors;

public class LockReleaseProcessor : IUnitOfWorkProcessor
{
    private readonly List<ILockableEntity> _toUnlock = [];

    public void Track(ILockableEntity entity)
    {
        if (!_toUnlock.Contains(entity))
            _toUnlock.Add(entity);
    }

    public Task BeforeSaveChangesAsync(AppDbContext context) 
        => Task.CompletedTask;

    public Task AfterSaveChangesAsync(AppDbContext context)
    {
        foreach (var entity in _toUnlock)
        {
            entity.Unlock();
        }

        _toUnlock.Clear();
        return context.SaveChangesAsync();
    }    
}