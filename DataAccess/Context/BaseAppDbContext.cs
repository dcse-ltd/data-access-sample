using DataAccess.Entity.Configuration.Extensions;
using DataAccess.Entity.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Context;

public abstract class BaseAppDbContext(DbContextOptions<BaseAppDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BaseAppDbContext).Assembly);
        ConfigureBehaviorsByConvention(modelBuilder);
    }
    
    private void ConfigureBehaviorsByConvention(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            var lockableInterface = clrType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && 
                                    i.GetGenericTypeDefinition() == typeof(ILockableEntity<>));

            var isConcurrent = typeof(IConcurrencyEntity).IsAssignableFrom(clrType);
            var isAuditable = typeof(IAuditableEntity).IsAssignableFrom(clrType);

            if (modelBuilder.Entity(clrType).Metadata.FindNavigation("Locking") != null ||
                modelBuilder.Entity(clrType).Metadata.FindNavigation("Auditing") != null ||
                modelBuilder.Entity(clrType).Metadata.FindNavigation("Concurrency") != null)
            {
                continue; 
            }

            if (lockableInterface != null)
            {
                var lockingMethod = typeof(LockingConfiguration)
                    .GetMethod(nameof(LockingConfiguration.ConfigureLocking))
                    ?.MakeGenericMethod(clrType);
                
                lockingMethod?.Invoke(null, [modelBuilder.Entity(clrType)]);
            }
            
            if (isAuditable)
            {
                var auditableMethod = typeof(AuditingConfiguration)
                    .GetMethod(nameof(AuditingConfiguration.ConfigureAuditing))
                    ?.MakeGenericMethod(clrType);
                
                auditableMethod?.Invoke(null, [modelBuilder.Entity(clrType)]);
            }

            if (!isConcurrent) 
                continue;
            
            var concurrencyMethod = typeof(ConcurrencyConfiguration)
                .GetMethod(nameof(ConcurrencyConfiguration.ConfigureConcurrency))
                ?.MakeGenericMethod(clrType);

            concurrencyMethod?.Invoke(null, [modelBuilder.Entity(clrType)]);
        }
    }
}