using DataAccess.Entity.Behaviors;
using DataAccess.Entity.Interfaces;

namespace Features.Order.Entities;

public class OrderProduct : 
    IEntity, 
    ILockableEntity<OrderProduct>, 
    IAuditableEntity,
    IConcurrencyEntity
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Order Order { get; set; }
    public Guid ProductId { get; set; }
    public Product.Entities.Product Product { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public LockingBehavior<OrderProduct> Locking { get; private set; } = new();
    public AuditingBehavior Auditing { get; private set; } = new();
    public ConcurrencyBehavior Concurrency { get; private set; } = new();
}