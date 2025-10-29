using Infrastructure.Entity.Behaviors;

namespace Infrastructure.Entity.Interfaces;

public interface IConcurrencyEntity 
{
    ConcurrencyBehavior Concurrency { get; }   
}