using Infrastructure.Entity.Behaviors;
using Infrastructure.Entity.Interfaces;

namespace Application.Customer.Entities;

public class Customer : 
    IEntity, 
    ILockableEntity<Customer>, 
    IAuditableEntity,
    IConcurrencyEntity,
    ISoftDeletableEntity
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public LockingBehavior<Customer> Locking { get; private set; } = new();
    public AuditingBehavior Auditing { get; private set; } = new();
    public ConcurrencyBehavior Concurrency { get; private set; } = new();
    public SoftDeleteBehavior Deleted { get; private set; } = new();
    public ICollection<Order.Entities.Order> Orders { get; set; } = new List<Order.Entities.Order>();   
}