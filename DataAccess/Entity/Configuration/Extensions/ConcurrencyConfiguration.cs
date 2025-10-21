using DataAccess.Entity.Interfaces;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Entity.Configuration.Extensions;

public static class ConcurrencyConfiguration
{
    public static void ConfigureConcurrency<TEntity>(
        this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, IConcurrencyEntity
    {
        builder.OwnsOne(e => e.Concurrency, concurrency =>
        {
            concurrency.Property(c => c.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken()
                .ValueGeneratedOnAddOrUpdate();
        });
    }
}