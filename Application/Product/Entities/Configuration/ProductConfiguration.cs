using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Product.Entities.Configuration;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        _ = builder
            .Property(x => x.Name)
            .HasMaxLength(128)
            .IsRequired();
        
        _ = builder
            .Property(x => x.Description)
            .HasMaxLength(1024)
            .IsRequired();

        _ = builder
            .HasIndex(x => x.Name)
            .IsUnique();

        _ = builder
            .Property(x => x.Price)
            .HasPrecision(18, 2);
    }
}