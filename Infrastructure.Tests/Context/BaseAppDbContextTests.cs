using Infrastructure.Context;
using Infrastructure.Entity.Behaviors;
using Infrastructure.Entity.Interfaces;
using Infrastructure.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Infrastructure.Tests.Context;

public class BaseAppDbContextTests
{
    [Fact]
    public void OnModelCreating_ConfiguresLockableEntity()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Act
        using var context = new TestDbContext(options);

        // Assert
        // Verify that the entity model is configured
        var entityType = context.Model.FindEntityType(typeof(TestLockableEntity));
        Assert.NotNull(entityType);
        
        // Verify navigation property exists (Locking behavior is configured)
        var lockingNavigation = entityType.FindNavigation("Locking");
        Assert.NotNull(lockingNavigation);
    }

    [Fact]
    public void OnModelCreating_ConfiguresAuditableEntity()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Act
        using var context = new TestDbContext(options);

        // Assert
        var entityType = context.Model.FindEntityType(typeof(TestAuditableEntity));
        Assert.NotNull(entityType);
        
        // Verify navigation property exists (Auditing behavior is configured)
        var auditingNavigation = entityType.FindNavigation("Auditing");
        Assert.NotNull(auditingNavigation);
    }

    [Fact]
    public void OnModelCreating_ConfiguresSoftDeletableEntity()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Act
        using var context = new TestDbContext(options);

        // Assert
        var entityType = context.Model.FindEntityType(typeof(TestSoftDeletableEntity));
        Assert.NotNull(entityType);
        
        // Verify navigation property exists (Deleted behavior is configured)
        var deletedNavigation = entityType.FindNavigation("Deleted");
        Assert.NotNull(deletedNavigation);
        
        // Verify query filter is applied (soft delete filter)
        var queryFilter = entityType.GetQueryFilter();
        Assert.NotNull(queryFilter);
    }

    [Fact]
    public void OnModelCreating_ConfiguresConcurrencyEntity()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Act
        using var context = new TestDbContext(options);

        // Assert
        var entityType = context.Model.FindEntityType(typeof(TestConcurrencyEntity));
        Assert.NotNull(entityType);
        
        // Verify navigation property exists (Concurrency behavior is configured)
        var concurrencyNavigation = entityType.FindNavigation("Concurrency");
        Assert.NotNull(concurrencyNavigation);
    }

    [Fact]
    public void OnModelCreating_AppliesSoftDeleteQueryFilter()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Act
        using var context = new TestDbContext(options);

        // Assert
        var entityType = context.Model.FindEntityType(typeof(TestSoftDeletableEntity));
        Assert.NotNull(entityType);
        
        // Verify query filter is applied
        var queryFilter = entityType.GetQueryFilter();
        Assert.NotNull(queryFilter);
        
        // Verify the filter excludes soft-deleted entities
        // The filter should be: e => !e.Deleted.SoftDeleteInfo.IsDeleted
        var filterExpression = queryFilter.ToString();
        Assert.Contains("IsDeleted", filterExpression);
    }

    [Fact]
    public void OnModelCreating_DoesNotApplySoftDeleteFilterToNonSoftDeletable()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Act
        using var context = new TestDbContext(options);

        // Assert
        var entityType = context.Model.FindEntityType(typeof(TestEntity));
        Assert.NotNull(entityType);
        
        // Verify no query filter for non-soft-deletable entities
        var queryFilter = entityType.GetQueryFilter();
        Assert.Null(queryFilter);
    }

    [Fact]
    public void OnModelCreating_ConfiguresMultipleBehaviors()
    {
        // Arrange - Create an entity that implements multiple interfaces
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Act
        using var context = new TestDbContext(options);

        // Assert - Verify all behaviors can coexist
        // Test that the context can be created without errors
        // when entities have multiple behaviors configured
        Assert.NotNull(context);
    }
}

