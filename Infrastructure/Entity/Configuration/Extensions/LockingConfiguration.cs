using Infrastructure.Entity.Interfaces;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Entity.Configuration.Extensions;

public static class LockingConfiguration
{
    public static void ConfigureLocking<TEntity>(
        this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, ILockableEntity<TEntity>, IEntity
    {
        builder.OwnsOne(e => e.Locking, locking =>
        {
            locking.OwnsOne(l => l.LockInfo, lockInfo =>
            {
                lockInfo.Property(li => li.LockedByUserId)
                    .IsRequired(false);
                
                lockInfo.Property(li => li.LockedAtUtc)
                    .IsRequired(false);
                
                lockInfo.Property(li => li.LockTimeoutMinutes)
                    .IsRequired();
            });
        });
    }
}