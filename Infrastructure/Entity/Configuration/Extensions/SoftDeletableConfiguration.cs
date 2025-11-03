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
            softDelete.OwnsOne(sd => sd.SoftDeleteInfo, softDeleteInfo =>
            {
                softDeleteInfo.Property(sdi => sdi.IsDeleted)
                    .IsRequired();
                
                softDeleteInfo.Property(sdi => sdi.DeletedAtUtc)
                    .IsRequired(false);

                softDeleteInfo.Property(sdi => sdi.DeletedByUserId)
                    .IsRequired(false);
                
                softDeleteInfo.HasIndex(sdi => sdi.IsDeleted);
            });
        });
    }
}