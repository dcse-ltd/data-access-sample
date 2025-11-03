using Infrastructure;
using Infrastructure.Repository.Interfaces;
using Infrastructure.Services.Interfaces;
using Infrastructure.Tests.Helpers;
using Infrastructure.UnitOfWork.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Tests;

public class ServiceCompositionTests
{
    [Fact]
    public void RegisterInfrastructure_RegistersAllServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.RegisterInfrastructure();

        // Assert - Verify registrations exist in service collection
        // Verify UnitOfWork
        var unitOfWorkDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IUnitOfWork));
        Assert.NotNull(unitOfWorkDescriptor);
        Assert.Equal(typeof(Infrastructure.UnitOfWork.UnitOfWork), unitOfWorkDescriptor.ImplementationType);

        // Verify Repository (generic service)
        var repositoryDescriptor = services.FirstOrDefault(s => 
            s.ServiceType.IsGenericType && 
            s.ServiceType.GetGenericTypeDefinition() == typeof(IRepository<>));
        Assert.NotNull(repositoryDescriptor);
        Assert.Equal(typeof(Infrastructure.Repository.Repository<>), repositoryDescriptor.ImplementationType);

        // Verify ConcurrencyService (generic service)
        var concurrencyServiceDescriptor = services.FirstOrDefault(s => 
            s.ServiceType.IsGenericType && 
            s.ServiceType.GetGenericTypeDefinition() == typeof(IConcurrencyService<>));
        Assert.NotNull(concurrencyServiceDescriptor);
        Assert.Equal(typeof(Infrastructure.Services.ConcurrencyService<>), concurrencyServiceDescriptor.ImplementationType);

        // Verify EntityAuditService (generic service)
        var auditServiceDescriptor = services.FirstOrDefault(s => 
            s.ServiceType.IsGenericType && 
            s.ServiceType.GetGenericTypeDefinition() == typeof(IEntityAuditService<>));
        Assert.NotNull(auditServiceDescriptor);
        Assert.Equal(typeof(Infrastructure.Services.EntityAuditService<>), auditServiceDescriptor.ImplementationType);

        // Verify EntityLockService (generic service)
        var lockServiceDescriptor = services.FirstOrDefault(s => 
            s.ServiceType.IsGenericType && 
            s.ServiceType.GetGenericTypeDefinition() == typeof(IEntityLockService<>));
        Assert.NotNull(lockServiceDescriptor);
        Assert.Equal(typeof(Infrastructure.Services.EntityLockService<>), lockServiceDescriptor.ImplementationType);

        // Verify EntitySoftDeleteService (generic service)
        var softDeleteServiceDescriptor = services.FirstOrDefault(s => 
            s.ServiceType.IsGenericType && 
            s.ServiceType.GetGenericTypeDefinition() == typeof(IEntitySoftDeleteService<>));
        Assert.NotNull(softDeleteServiceDescriptor);
        Assert.Equal(typeof(Infrastructure.Services.EntitySoftDeleteService<>), softDeleteServiceDescriptor.ImplementationType);

        // Verify CoreEntityService (generic service)
        var coreEntityServiceDescriptor = services.FirstOrDefault(s => 
            s.ServiceType.IsGenericType && 
            s.ServiceType.GetGenericTypeDefinition() == typeof(ICoreEntityService<>));
        Assert.NotNull(coreEntityServiceDescriptor);
        Assert.Equal(typeof(Infrastructure.Services.CoreEntityService<>), coreEntityServiceDescriptor.ImplementationType);

        // Verify CollectionSyncService
        var collectionSyncServiceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ICollectionSyncService));
        Assert.NotNull(collectionSyncServiceDescriptor);
        Assert.Equal(typeof(Infrastructure.Services.CollectionSyncService), collectionSyncServiceDescriptor.ImplementationType);
    }

    [Fact]
    public void RegisterInfrastructure_RegistersServicesAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
        var loggerFactory = LoggerFactory.Create(b => { });
        services.AddScoped<ILogger<Infrastructure.UnitOfWork.UnitOfWork>>(
            sp => loggerFactory.CreateLogger<Infrastructure.UnitOfWork.UnitOfWork>());
        services.AddScoped(typeof(ILogger<>), typeof(Logger<>));

        // Act
        services.RegisterInfrastructure();

        // Assert
        var unitOfWorkDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IUnitOfWork));
        Assert.NotNull(unitOfWorkDescriptor);
        Assert.Equal(ServiceLifetime.Scoped, unitOfWorkDescriptor.Lifetime);

        var repositoryDescriptor = services.FirstOrDefault(s => 
            s.ServiceType.IsGenericType && 
            s.ServiceType.GetGenericTypeDefinition() == typeof(IRepository<>));
        Assert.NotNull(repositoryDescriptor);
        Assert.Equal(ServiceLifetime.Scoped, repositoryDescriptor.Lifetime);

        var collectionSyncDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ICollectionSyncService));
        Assert.NotNull(collectionSyncDescriptor);
        Assert.Equal(ServiceLifetime.Scoped, collectionSyncDescriptor.Lifetime);
    }

    [Fact]
    public void RegisterInfrastructure_RegistersServicesWithCorrectImplementations()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.RegisterInfrastructure();

        // Assert - Verify implementation types match
        // Verify UnitOfWork implementation
        var unitOfWorkDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IUnitOfWork));
        Assert.NotNull(unitOfWorkDescriptor);
        Assert.Equal(typeof(Infrastructure.UnitOfWork.UnitOfWork), unitOfWorkDescriptor.ImplementationType);

        // Verify Repository implementation
        var repositoryDescriptor = services.FirstOrDefault(s => 
            s.ServiceType.IsGenericType && 
            s.ServiceType.GetGenericTypeDefinition() == typeof(IRepository<>));
        Assert.NotNull(repositoryDescriptor);
        Assert.Equal(typeof(Infrastructure.Repository.Repository<>), repositoryDescriptor.ImplementationType);

        // Verify CollectionSyncService implementation
        var collectionSyncServiceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ICollectionSyncService));
        Assert.NotNull(collectionSyncServiceDescriptor);
        Assert.Equal(typeof(Infrastructure.Services.CollectionSyncService), collectionSyncServiceDescriptor.ImplementationType);
    }
}

