using Infrastructure.Entity.Behaviors;

namespace Infrastructure.Entity.Interfaces;

public interface ISoftDeletableEntity
{
    public SoftDeleteBehavior Deleted { get; }   
}