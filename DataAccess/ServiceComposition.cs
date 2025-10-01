using DataAccess.Context;
using DataAccess.Repository;
using DataAccess.Repository.Interfaces;
using DataAccess.Services;
using DataAccess.Services.Interfaces;
using DataAccess.UnitOfWork.Interfaces;
using DataAccess.UnitOfWork.Processors;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccess;

public static class ServiceComposition
{
    public static void RegisterDataAccess(IServiceCollection services)
    {
        services.AddScoped<AppDbContext>();
        services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();
        services.AddScoped<IUnitOfWorkProcessor, AuditStampProcessor>();
        services.AddScoped<IUnitOfWorkProcessor, LockReleaseProcessor>();

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IEntityLockService, EntityLockService>();
    }
}