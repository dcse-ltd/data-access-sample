using DataAccess.Entity.Behaviors;

namespace DataAccess.Entity.Interfaces;

public interface IConcurrencyEntity 
{
    ConcurrencyBehavior Concurrency { get; }   
}