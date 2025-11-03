using Infrastructure.Entity.Behaviors;
using Infrastructure.Entity.Interfaces;

namespace Infrastructure.Tests.Helpers;

public class TestConcurrencyEntity : IEntity, IConcurrencyEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ConcurrencyBehavior Concurrency { get; } = new();
}

