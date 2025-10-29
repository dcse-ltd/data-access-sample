using Infrastructure.Entity.Interfaces;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Entity.Configuration.Extensions;

public static class SoftDeletableConfiguration
{
    public static void ConfigureSoftDeletable<TEntity>(
        this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, ISoftDeletableEntity
    {
        builder.OwnsOne(e => e.Deleted, softDelete =>
        {
            softDelete.Property(a => a.SoftDeleteInfo.IsDeleted)
                .IsRequired();
            
            softDelete.Property(a => a.SoftDeleteInfo.DeletedAtUtc)
                .IsRequired(false);

            softDelete.Property(a => a.SoftDeleteInfo.DeletedByUserId)
                .IsRequired(false);
            
            softDelete.HasIndex(a => a.SoftDeleteInfo.IsDeleted);
        });
    }
}