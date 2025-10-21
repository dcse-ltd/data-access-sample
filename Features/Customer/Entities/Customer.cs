using DataAccess.Entity.Behaviors;
using DataAccess.Entity.Interfaces;

namespace Features.Customer.Entities;

public class Customer : 
    IEntity, 
    ILockableEntity<Customer>, 
    IAuditableEntity,
    IConcurrencyEntity
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public LockingBehavior<Customer> Locking { get; private set; } = new();
    public AuditingBehavior Auditing { get; private set; } = new();
    public ConcurrencyBehavior Concurrency { get; private set; } = new();
    public ICollection<Order.Entities.Order> Orders { get; set; }
}