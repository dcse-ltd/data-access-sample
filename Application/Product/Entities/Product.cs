using Application.Order.Entities;
using Infrastructure.Entity.Behaviors;
using Infrastructure.Entity.Interfaces;

namespace Application.Product.Entities;

public class Product : 
    IEntity, 
    ILockableEntity<Product>, 
    IAuditableEntity,
    IConcurrencyEntity,
    ISoftDeletableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public LockingBehavior<Product> Locking { get; private set; } = new();
    public AuditingBehavior Auditing { get; private set; } = new();
    public ConcurrencyBehavior Concurrency { get; private set; } = new();
    public SoftDeleteBehavior Deleted { get; private set; } = new();
    public ICollection<OrderProduct> OrderProducts { get; set; } = new List<OrderProduct>();   
}