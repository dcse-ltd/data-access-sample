using DataAccess.Context;
using Features.Order.Entities;
using Microsoft.EntityFrameworkCore;

namespace Features;

public class AppDbContext(DbContextOptions<BaseAppDbContext> options) 
    : BaseAppDbContext(options)
{
    public DbSet<Customer.Entities.Customer> Customers { get; set; }
    public DbSet<Order.Entities.Order> Orders { get; set; }
    public DbSet<Product.Entities.Product> Products { get; set; }
    public DbSet<OrderProduct> OrderProducts { get; set; }
}