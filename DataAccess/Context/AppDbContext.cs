using DataAccess.Entity;
using DataAccess.Entity.Configuration;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Context;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<OrderProduct> OrderProducts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .ApplyConfiguration(new CustomerConfiguration())
            .ApplyConfiguration(new OrderConfiguration())
            .ApplyConfiguration(new ProductConfiguration())
            .ApplyConfiguration(new OrderProductConfiguration());
    }
}