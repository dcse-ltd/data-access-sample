using DataAccess.Context;
using DataAccess.Entity.Interfaces;
using DataAccess.Services.Interfaces;
using DataAccess.UnitOfWork.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.UnitOfWork.Processors;

public class AuditStampProcessor(ICurrentUserService currentUser) : IUnitOfWorkProcessor
{
    public Task BeforeSaveChangesAsync(AppDbContext context)
    {
        var userId = currentUser.UserId;
        var now = DateTime.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.AuditInfo.CreatedOn = now;
                    entry.Entity.AuditInfo.CreatedBy = userId;
                    break;
                case EntityState.Modified:
                    entry.Entity.AuditInfo.ModifiedOn = now;
                    entry.Entity.AuditInfo.ModifiedBy = userId;
                    break;
            }
        }

        return Task.CompletedTask;
    }

    public Task AfterSaveChangesAsync(AppDbContext context) 
        => Task.CompletedTask;
}