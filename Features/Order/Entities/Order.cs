using DataAccess.Entity.Behaviors;
using DataAccess.Entity.Interfaces;

namespace Features.Order.Entities;

public class Order : 
    IEntity, 
    ILockableEntity<Order>, 
    IAuditableEntity,
    IConcurrencyEntity
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public Guid CustomerId { get; set; }
    public Customer.Entities.Customer Customer { get; set; }
    public LockingBehavior<Order> Locking { get; private set; } = new();
    public AuditingBehavior Auditing { get; private set; } = new();
    public ConcurrencyBehavior Concurrency { get; private set; } = new();
    public ICollection<OrderProduct> OrderProducts { get; set; }
}