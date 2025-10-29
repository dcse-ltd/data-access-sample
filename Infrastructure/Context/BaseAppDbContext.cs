using System.Reflection;
using Infrastructure.Entity.Configuration.Extensions;
using Infrastructure.Entity.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Context;

public abstract class BaseAppDbContext(
    DbContextOptions options,
    Assembly configurationAssembly) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BaseAppDbContext).Assembly);
        modelBuilder.ApplyConfigurationsFromAssembly(configurationAssembly);
        ConfigureBehaviorsByConvention(modelBuilder);
        ApplySoftDeleteQueryFilter(modelBuilder);
    }
    
    private static void ConfigureBehaviorsByConvention(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            var lockableInterface = clrType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && 
                                    i.GetGenericTypeDefinition() == typeof(ILockableEntity<>));

            var isConcurrent = typeof(IConcurrencyEntity).IsAssignableFrom(clrType);
            var isAuditable = typeof(IAuditableEntity).IsAssignableFrom(clrType);
            var isSoftDeletable = typeof(ISoftDeletableEntity).IsAssignableFrom(clrType);

            if (modelBuilder.Entity(clrType).Metadata.FindNavigation("Locking") != null ||
                modelBuilder.Entity(clrType).Metadata.FindNavigation("Auditing") != null ||
                modelBuilder.Entity(clrType).Metadata.FindNavigation("Concurrency") != null ||
                modelBuilder.Entity(clrType).Metadata.FindNavigation("Deleted") != null)
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
            
            if (isSoftDeletable)
            {
                var softDeletableMethod = typeof(SoftDeletableConfiguration)
                    .GetMethod(nameof(SoftDeletableConfiguration.ConfigureSoftDeletable))
                    ?.MakeGenericMethod(clrType);
                
                softDeletableMethod?.Invoke(null, [modelBuilder.Entity(clrType)]);
            }

            if (!isConcurrent) 
                continue;
            
            var concurrencyMethod = typeof(ConcurrencyConfiguration)
                .GetMethod(nameof(ConcurrencyConfiguration.ConfigureConcurrency))
                ?.MakeGenericMethod(clrType);

            concurrencyMethod?.Invoke(null, [modelBuilder.Entity(clrType)]);
        }
    }
    
    private static void ApplySoftDeleteQueryFilter(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDeletableEntity).IsAssignableFrom(entityType.ClrType))
                continue;

            var method = typeof(BaseAppDbContext)
                .GetMethod(nameof(SetSoftDeleteFilter), 
                    BindingFlags.NonPublic | BindingFlags.Static)?
                .MakeGenericMethod(entityType.ClrType);

            method?.Invoke(null, [modelBuilder]);
        }
    }

    private static void SetSoftDeleteFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ISoftDeletableEntity
    {
        modelBuilder.Entity<TEntity>()
            .HasQueryFilter(e => !e.Deleted.SoftDeleteInfo.IsDeleted);
    }
}