using Infrastructure.Services;
using Infrastructure.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Infrastructure.Tests.Services;

public class EntityAuditServiceTests
{
    private readonly Mock<ILogger<EntityAuditService<TestAuditableEntity>>> _loggerMock;
    private readonly EntityAuditService<TestAuditableEntity> _service;

    public EntityAuditServiceTests()
    {
        _loggerMock = new Mock<ILogger<EntityAuditService<TestAuditableEntity>>>();
        _service = new EntityAuditService<TestAuditableEntity>(_loggerMock.Object);
    }

    [Fact]
    public void StampForCreate_WithAuditableEntity_SetsCreatedAndModified()
    {
        // Arrange
        var entity = new TestAuditableEntity { Id = Guid.NewGuid() };
        var userId = Guid.NewGuid();
        var beforeTime = DateTime.UtcNow;

        // Act
        _service.StampForCreate(entity, userId);
        var afterTime = DateTime.UtcNow;

        // Assert
        Assert.Equal(userId, entity.Auditing.AuditInfo.CreatedByUserId);
        Assert.Equal(userId, entity.Auditing.AuditInfo.ModifiedByUserId);
        Assert.InRange(entity.Auditing.AuditInfo.CreatedAtUtc, beforeTime, afterTime);
        Assert.InRange(entity.Auditing.AuditInfo.ModifiedAtUtc, beforeTime, afterTime);
    }

    [Fact]
    public void StampForCreate_WithNonAuditableEntity_DoesNothing()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<EntityAuditService<TestEntity>>>();
        var service = new EntityAuditService<TestEntity>(loggerMock.Object);
        var entity = new TestEntity { Id = Guid.NewGuid() };
        var userId = Guid.NewGuid();

        // Act
        service.StampForCreate(entity, userId);

        // Assert
        // Should not throw and should do nothing
        Assert.NotNull(entity);
    }

    [Fact]
    public void StampForUpdate_WithAuditableEntity_SetsModified()
    {
        // Arrange
        var entity = new TestAuditableEntity { Id = Guid.NewGuid() };
        var originalCreatedBy = Guid.NewGuid();
        var originalCreatedAt = DateTime.UtcNow.AddDays(-1);
        entity.Auditing.AuditInfo.CreatedByUserId = originalCreatedBy;
        entity.Auditing.AuditInfo.CreatedAtUtc = originalCreatedAt;
        
        var userId = Guid.NewGuid();
        var beforeTime = DateTime.UtcNow;

        // Act
        _service.StampForUpdate(entity, userId);
        var afterTime = DateTime.UtcNow;

        // Assert
        Assert.Equal(originalCreatedBy, entity.Auditing.AuditInfo.CreatedByUserId);
        Assert.Equal(originalCreatedAt, entity.Auditing.AuditInfo.CreatedAtUtc);
        Assert.Equal(userId, entity.Auditing.AuditInfo.ModifiedByUserId);
        Assert.InRange(entity.Auditing.AuditInfo.ModifiedAtUtc, beforeTime, afterTime);
    }

    [Fact]
    public void StampForUpdate_WithNonAuditableEntity_DoesNothing()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<EntityAuditService<TestEntity>>>();
        var service = new EntityAuditService<TestEntity>(loggerMock.Object);
        var entity = new TestEntity { Id = Guid.NewGuid() };
        var userId = Guid.NewGuid();

        // Act
        service.StampForUpdate(entity, userId);

        // Assert
        // Should not throw and should do nothing
        Assert.NotNull(entity);
    }
}

