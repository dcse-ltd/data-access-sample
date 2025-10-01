using DataAccess.Context;

namespace DataAccess.UnitOfWork.Interfaces;

public interface IUnitOfWorkProcessor
{
    Task BeforeSaveChangesAsync(AppDbContext context);
    Task AfterSaveChangesAsync(AppDbContext context);
}