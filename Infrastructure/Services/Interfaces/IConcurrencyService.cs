using Infrastructure.Entity.Interfaces;
using Infrastructure.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services.Interfaces;

public interface IConcurrencyService<in TEntity>
    where TEntity : class, IEntity
{
    ConcurrencyException HandleConcurrencyException(
        TEntity entity,
        DbUpdateConcurrencyException exception);
}