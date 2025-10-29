using Application.Order.Entities;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Application;

public class AppDbContext(DbContextOptions<BaseAppDbContext> options) 
    : BaseAppDbContext(options, typeof(AppDbContext).Assembly)
{
    public DbSet<Customer.Entities.Customer> Customers { get; set; }
    public DbSet<Order.Entities.Order> Orders { get; set; }
    public DbSet<Product.Entities.Product> Products { get; set; }
    public DbSet<OrderProduct> OrderProducts { get; set; }
}