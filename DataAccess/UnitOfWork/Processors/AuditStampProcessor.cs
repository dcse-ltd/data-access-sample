using DataAccess.Context;
using DataAccess.Entity.Interfaces;
using DataAccess.Services.Interfaces;
using DataAccess.UnitOfWork.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.UnitOfWork.Processors;

public class AuditStampProcessor(ICurrentUserService currentUser) : IUnitOfWorkProcessor
{
    public Task BeforeSaveChangesAsync(BaseAppDbContext context)
    {
        var userId = currentUser.UserId;
        var now = DateTime.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.Auditing.AuditInfo.CreatedAtUtc = now;
                    entry.Entity.Auditing.AuditInfo.CreatedByUserId = userId;
                    break;
                case EntityState.Modified:
                    entry.Entity.Auditing.AuditInfo.ModifiedAtUtc = now;
                    entry.Entity.Auditing.AuditInfo.ModifiedByUserId = userId;
                    break;
            }
        }

        return Task.CompletedTask;
    }

    public Task AfterSaveChangesAsync(BaseAppDbContext context) 
        => Task.CompletedTask;
}