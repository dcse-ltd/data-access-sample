using System.Reflection;
using Infrastructure.Entity.Configuration.Extensions;
using Infrastructure.Entity.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;

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
        // Materialize the collection first to avoid "collection modified" errors
        var entityTypes = modelBuilder.Model.GetEntityTypes().ToList();
        
        foreach (var entityType in entityTypes)
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
                
                // Get the generic Entity<TEntity>() method
                var entityMethod = typeof(ModelBuilder).GetMethod(nameof(ModelBuilder.Entity), Type.EmptyTypes)
                    ?.MakeGenericMethod(clrType);
                var entityBuilder = entityMethod?.Invoke(modelBuilder, null);
                
                lockingMethod?.Invoke(null, [entityBuilder]);
            }
            
            if (isAuditable)
            {
                var auditableMethod = typeof(AuditingConfiguration)
                    .GetMethod(nameof(AuditingConfiguration.ConfigureAuditing))
                    ?.MakeGenericMethod(clrType);
                
                // Get the generic Entity<TEntity>() method
                var entityMethod = typeof(ModelBuilder).GetMethod(nameof(ModelBuilder.Entity), Type.EmptyTypes)
                    ?.MakeGenericMethod(clrType);
                var entityBuilder = entityMethod?.Invoke(modelBuilder, null);
                
                auditableMethod?.Invoke(null, [entityBuilder]);
            }
            
            if (isSoftDeletable)
            {
                var softDeletableMethod = typeof(SoftDeletableConfiguration)
                    .GetMethod(nameof(SoftDeletableConfiguration.ConfigureSoftDeletable))
                    ?.MakeGenericMethod(clrType);
                
                // Get the generic Entity<TEntity>() method
                var entityMethod = typeof(ModelBuilder).GetMethod(nameof(ModelBuilder.Entity), Type.EmptyTypes)
                    ?.MakeGenericMethod(clrType);
                var entityBuilder = entityMethod?.Invoke(modelBuilder, null);
                
                softDeletableMethod?.Invoke(null, [entityBuilder]);
            }

            if (!isConcurrent) 
                continue;
            
            var concurrencyMethod = typeof(ConcurrencyConfiguration)
                .GetMethod(nameof(ConcurrencyConfiguration.ConfigureConcurrency))
                ?.MakeGenericMethod(clrType);

            // Get the generic Entity<TEntity>() method
            var entityMethod2 = typeof(ModelBuilder).GetMethod(nameof(ModelBuilder.Entity), Type.EmptyTypes)
                ?.MakeGenericMethod(clrType);
            var entityBuilder2 = entityMethod2?.Invoke(modelBuilder, null);

            concurrencyMethod?.Invoke(null, [entityBuilder2]);
        }
    }
    
    private static void ApplySoftDeleteQueryFilter(ModelBuilder modelBuilder)
    {
        // Materialize the collection first to avoid "collection modified" errors
        var entityTypes = modelBuilder.Model.GetEntityTypes().ToList();
        
        foreach (var entityType in entityTypes)
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