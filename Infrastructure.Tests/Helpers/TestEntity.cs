using Infrastructure.Entity.Interfaces;

namespace Infrastructure.Tests.Helpers;

public class TestEntity : IEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int Value { get; set; }
}

