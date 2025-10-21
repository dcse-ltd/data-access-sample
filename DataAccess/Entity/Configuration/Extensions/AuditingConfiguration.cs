using DataAccess.Entity.Interfaces;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Entity.Configuration.Extensions;

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
        });
    }
}