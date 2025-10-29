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
            auditing.Property(a => a.AuditInfo.CreatedByUserId)
                .IsRequired();
            
            auditing.Property(a => a.AuditInfo.CreatedAtUtc)
                .IsRequired();
            
            auditing.Property(a => a.AuditInfo.ModifiedByUserId)
                .IsRequired(false);
            
            auditing.Property(a => a.AuditInfo.ModifiedAtUtc)
                .IsRequired(false);
            
            auditing.HasIndex(a => a.AuditInfo.CreatedAtUtc);
            
            auditing.HasIndex(a => new { a.AuditInfo.ModifiedAtUtc, a.AuditInfo.ModifiedByUserId });
        });
    }
}