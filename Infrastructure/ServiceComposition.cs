using Infrastructure.Repository;
using Infrastructure.Repository.Interfaces;
using Infrastructure.Services;
using Infrastructure.Services.Interfaces;
using Infrastructure.UnitOfWork.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class ServiceComposition
{
    public static void RegisterInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        services.AddScoped(typeof(IConcurrencyService<>), typeof(ConcurrencyService<>));
        services.AddScoped(typeof(IEntityAuditService<>), typeof(EntityAuditService<>));
        services.AddScoped(typeof(IEntityLockService<>), typeof(EntityLockService<>));
        services.AddScoped(typeof(IEntitySoftDeleteService<>), typeof(EntitySoftDeleteService<>));
        services.AddScoped(typeof(ICoreEntityService<>), typeof(CoreEntityService<>));
    }
}