using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Customer.Entities.Configuration;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder
            .Property(x => x.FirstName)
            .IsRequired()
            .HasMaxLength(64);
        
        builder
            .Property(x => x.LastName)
            .IsRequired()
            .HasMaxLength(64);

        builder
            .Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(128);
        
        builder
            .Property(x => x.Phone)
            .IsRequired()
            .HasMaxLength(16);

        builder
            .HasIndex(x => x.Email)
            .IsUnique();

        builder
            .HasIndex(x => x.Email);

        builder
            .HasIndex(x => x.LastName);

        builder
            .HasIndex(x => x.Phone);
    }
}