using Infrastructure.Entity.Attributes;
using Infrastructure.Entity.Behaviors;
using Infrastructure.Entity.Interfaces;

namespace Application.Order.Entities;

public class Order : 
    IEntity, 
    ILockableEntity<Order>, 
    IAuditableEntity,
    IConcurrencyEntity,
    ISoftDeletableEntity
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public Guid CustomerId { get; set; }
    public Customer.Entities.Customer Customer { get; set; } = null!;
    public LockingBehavior<Order> Locking { get; private set; } = new();
    public AuditingBehavior Auditing { get; private set; } = new();
    public ConcurrencyBehavior Concurrency { get; private set; } = new();
    public SoftDeleteBehavior Deleted { get; private set; } = new();   
    
    [CascadeLock]
    [CascadeDelete]
    public ICollection<OrderProduct> OrderProducts { get; set; } = new List<OrderProduct>();
}