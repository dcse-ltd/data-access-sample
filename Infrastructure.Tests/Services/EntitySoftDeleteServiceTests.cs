using Infrastructure.Services;
using Infrastructure.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Infrastructure.Tests.Services;

public class EntitySoftDeleteServiceTests
{
    private readonly Mock<ILogger<EntitySoftDeleteService<TestSoftDeletableEntity>>> _loggerMock;
    private readonly EntitySoftDeleteService<TestSoftDeletableEntity> _service;

    public EntitySoftDeleteServiceTests()
    {
        _loggerMock = new Mock<ILogger<EntitySoftDeleteService<TestSoftDeletableEntity>>>();
        _service = new EntitySoftDeleteService<TestSoftDeletableEntity>(_loggerMock.Object);
    }

    [Fact]
    public void StampForDelete_WithSoftDeletableEntity_MarksAsDeleted()
    {
        // Arrange
        var entity = new TestSoftDeletableEntity { Id = Guid.NewGuid() };
        var userId = Guid.NewGuid();
        var beforeTime = DateTime.UtcNow;

        // Act
        _service.StampForDelete(entity, userId);
        var afterTime = DateTime.UtcNow;

        // Assert
        Assert.True(entity.Deleted.SoftDeleteInfo.IsDeleted);
        Assert.Equal(userId, entity.Deleted.SoftDeleteInfo.DeletedByUserId);
        Assert.NotNull(entity.Deleted.SoftDeleteInfo.DeletedAtUtc);
        Assert.InRange(entity.Deleted.SoftDeleteInfo.DeletedAtUtc.Value, beforeTime, afterTime);
    }

    [Fact]
    public void StampForDelete_WithNonSoftDeletableEntity_DoesNothing()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<EntitySoftDeleteService<TestEntity>>>();
        var service = new EntitySoftDeleteService<TestEntity>(loggerMock.Object);
        var entity = new TestEntity { Id = Guid.NewGuid() };
        var userId = Guid.NewGuid();

        // Act
        service.StampForDelete(entity, userId);

        // Assert
        // Should not throw and should do nothing
        Assert.NotNull(entity);
    }

    [Fact]
    public void StampForRestore_WithSoftDeletableEntity_RestoresEntity()
    {
        // Arrange
        var entity = new TestSoftDeletableEntity { Id = Guid.NewGuid() };
        var userId = Guid.NewGuid();
        _service.StampForDelete(entity, userId);
        Assert.True(entity.Deleted.SoftDeleteInfo.IsDeleted);

        // Act
        _service.StampForRestore(entity);

        // Assert
        Assert.False(entity.Deleted.SoftDeleteInfo.IsDeleted);
        Assert.Null(entity.Deleted.SoftDeleteInfo.DeletedByUserId);
        Assert.Null(entity.Deleted.SoftDeleteInfo.DeletedAtUtc);
    }

    [Fact]
    public void StampForRestore_WithNonSoftDeletableEntity_DoesNothing()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<EntitySoftDeleteService<TestEntity>>>();
        var service = new EntitySoftDeleteService<TestEntity>(loggerMock.Object);
        var entity = new TestEntity { Id = Guid.NewGuid() };

        // Act
        service.StampForRestore(entity);

        // Assert
        // Should not throw and should do nothing
        Assert.NotNull(entity);
    }

    [Fact]
    public void StampForDeleteWithChildren_SoftDeletesEntity()
    {
        // Arrange
        var entity = new TestSoftDeletableEntity { Id = Guid.NewGuid() };
        var userId = Guid.NewGuid();

        // Act
        _service.StampForDeleteWithChildren(entity, userId);

        // Assert
        Assert.True(entity.Deleted.SoftDeleteInfo.IsDeleted);
        Assert.Equal(userId, entity.Deleted.SoftDeleteInfo.DeletedByUserId);
    }

    [Fact]
    public void StampForRestoreWithChildren_RestoresEntity()
    {
        // Arrange
        var entity = new TestSoftDeletableEntity { Id = Guid.NewGuid() };
        var userId = Guid.NewGuid();
        _service.StampForDelete(entity, userId);
        Assert.True(entity.Deleted.SoftDeleteInfo.IsDeleted);

        // Act
        _service.StampForRestoreWithChildren(entity);

        // Assert
        Assert.False(entity.Deleted.SoftDeleteInfo.IsDeleted);
        Assert.Null(entity.Deleted.SoftDeleteInfo.DeletedByUserId);
    }
}

