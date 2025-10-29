using Infrastructure.Entity.Behaviors;
using Infrastructure.Entity.Interfaces;

namespace Application.Order.Entities;

public class OrderProduct : 
    IEntity, 
    ILockableEntity<OrderProduct>, 
    IAuditableEntity,
    IConcurrencyEntity,
    ISoftDeletableEntity
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product.Entities.Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public LockingBehavior<OrderProduct> Locking { get; private set; } = new();
    public AuditingBehavior Auditing { get; private set; } = new();
    public ConcurrencyBehavior Concurrency { get; private set; } = new();
    public SoftDeleteBehavior Deleted { get; private set; } = new();   
}