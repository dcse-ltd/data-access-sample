using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Order.Entities.Configuration;

public class OrderProductConfiguration : IEntityTypeConfiguration<OrderProduct>
{
    public void Configure(EntityTypeBuilder<OrderProduct> builder)
    {
        builder.Property(x => x.Quantity)
            .IsRequired();
        
        builder
            .Property(x => x.Price)
            .HasPrecision(18, 2);
        
        builder
            .HasOne(x => x.Order)
            .WithMany(x => x.OrderProducts)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder
            .HasOne(x => x.Product)
            .WithMany(x => x.OrderProducts)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(op => op.OrderId);
        builder.HasIndex(op => op.ProductId);
        
        builder
            .HasIndex(x => new { x.OrderId, x.ProductId })
            .IsUnique();
    }
}