using DataAccess.Entity.Behaviors;
using DataAccess.Entity.Interfaces;
using Features.Order.Entities;

namespace Features.Product.Entities;

public class Product : 
    IEntity, 
    ILockableEntity<Product>, 
    IAuditableEntity,
    IConcurrencyEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public LockingBehavior<Product> Locking { get; private set; } = new();
    public AuditingBehavior Auditing { get; private set; } = new();
    public ConcurrencyBehavior Concurrency { get; private set; } = new();
    public ICollection<OrderProduct> OrderProducts { get; set; }
}