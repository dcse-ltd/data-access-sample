using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Product.Entities.Configuration;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder
            .Property(x => x.Name)
            .HasMaxLength(128)
            .IsRequired();
        
        builder
            .Property(x => x.Description)
            .HasMaxLength(1024)
            .IsRequired();

        builder
            .HasIndex(x => x.Name)
            .IsUnique();

        builder
            .Property(x => x.Price)
            .HasPrecision(18, 2);

        builder
            .HasIndex(x => x.Name)
            .IsUnique();
    }
}