using DataAccess.Context;

namespace DataAccess.UnitOfWork.Interfaces;

public interface IUnitOfWorkProcessor
{
    Task BeforeSaveChangesAsync(BaseAppDbContext context);
    Task AfterSaveChangesAsync(BaseAppDbContext context);
}