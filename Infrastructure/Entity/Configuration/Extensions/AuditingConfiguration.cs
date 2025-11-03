using Infrastructure.Entity.Interfaces;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Entity.Configuration.Extensions;

public static class AuditingConfiguration
{
    public static void ConfigureAuditing<TEntity>(
        this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, IAuditableEntity
    {
        builder.OwnsOne(e => e.Auditing, auditing =>
        {
            auditing.OwnsOne(a => a.AuditInfo, auditInfo =>
            {
                auditInfo.Property(ai => ai.CreatedByUserId)
                    .IsRequired();
                
                auditInfo.Property(ai => ai.CreatedAtUtc)
                    .IsRequired();
                
                // ModifiedByUserId and ModifiedAtUtc are non-nullable types, so they are required by default
                // They will be set when the entity is modified
                
                auditInfo.HasIndex(ai => ai.CreatedAtUtc);
                
                auditInfo.HasIndex(ai => new { ai.ModifiedAtUtc, ai.ModifiedByUserId });
            });
        });
    }
}