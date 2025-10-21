using DataAccess.Context;
using DataAccess.Entity.Interfaces;
using DataAccess.Services.Interfaces;
using DataAccess.UnitOfWork.Interfaces;

namespace DataAccess.UnitOfWork.Processors;

public class LockReleaseProcessor(ICurrentUserService currentUserService) 
    : IUnitOfWorkProcessor
{
    private readonly List<UnlockAction> _toUnlock = [];

    public void Track<TEntity>(TEntity entity) 
        where TEntity : class, IEntity
    {
        if (entity is not ILockableEntity<TEntity> lockable) 
            return;
        
        var userId = currentUserService.UserId;
            
        if (_toUnlock.All(a => a.Entity != entity))
        {
            _toUnlock.Add(new UnlockAction
            {
                Entity = entity,
                UserId = userId,
                UnlockFunc = () => lockable.Locking.Unlock(userId)
            });
        }
    }

    public async Task BeforeSaveChangesAsync(BaseAppDbContext context) 
    {
        try
        {
            foreach (var action in _toUnlock)
            {
                action.UnlockFunc();
            }

            _toUnlock.Clear();
            await context.SaveChangesAsync();
        }
        catch
        {
            _toUnlock.Clear();
            throw;
        }
    }  

    public Task AfterSaveChangesAsync(BaseAppDbContext context)
        => Task.CompletedTask;

    private class UnlockAction
    {
        public object Entity { get; set; } = null!;
        public Guid UserId { get; set; }
        public Action UnlockFunc { get; set; } = null!;
    }
}