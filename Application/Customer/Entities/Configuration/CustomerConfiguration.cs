using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Customer.Entities.Configuration;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        _ = builder
            .Property(x => x.FirstName)
            .IsRequired()
            .HasMaxLength(64);
        
        _ = builder
            .Property(x => x.LastName)
            .IsRequired()
            .HasMaxLength(64);

        _ = builder
            .Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(128);
        
        _ = builder
            .Property(x => x.Phone)
            .IsRequired()
            .HasMaxLength(16);

        _ = builder
            .HasIndex(x => x.Email)
            .IsUnique();
    }
}