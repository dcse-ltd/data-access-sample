using Infrastructure.Exceptions;
using Infrastructure.Services;
using Infrastructure.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Infrastructure.Tests.Services;

public class EntityLockServiceTests
{
    private readonly Mock<ILogger<EntityLockService<TestLockableEntity>>> _loggerMock;
    private readonly EntityLockService<TestLockableEntity> _service;

    public EntityLockServiceTests()
    {
        _loggerMock = new Mock<ILogger<EntityLockService<TestLockableEntity>>>();
        _service = new EntityLockService<TestLockableEntity>(_loggerMock.Object);
    }

    [Fact]
    public void LockIfSupported_WithLockableEntity_LocksEntity()
    {
        // Arrange
        var entity = new TestLockableEntity { Id = Guid.NewGuid() };
        var userId = Guid.NewGuid();

        // Act
        _service.LockIfSupported(entity, userId);

        // Assert
        Assert.True(entity.Locking.LockInfo.IsLocked());
        Assert.True(entity.Locking.LockInfo.IsLockedBy(userId));
    }

    [Fact]
    public void LockIfSupported_WithNonLockableEntity_DoesNothing()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<EntityLockService<TestEntity>>>();
        var service = new EntityLockService<TestEntity>(loggerMock.Object);
        var entity = new TestEntity { Id = Guid.NewGuid() };
        var userId = Guid.NewGuid();

        // Act
        service.LockIfSupported(entity, userId);

        // Assert
        // Should not throw
        Assert.NotNull(entity);
    }

    [Fact]
    public void UnlockIfSupported_WithLockableEntityAndCorrectUser_ReturnsTrue()
    {
        // Arrange
        var entity = new TestLockableEntity { Id = Guid.NewGuid() };
        var userId = Guid.NewGuid();
        _service.LockIfSupported(entity, userId);

        // Act
        var result = _service.UnlockIfSupported(entity, userId);

        // Assert
        Assert.True(result);
        Assert.False(entity.Locking.LockInfo.IsLocked());
    }

    [Fact]
    public void UnlockIfSupported_WithLockableEntityAndWrongUser_ReturnsFalse()
    {
        // Arrange
        var entity = new TestLockableEntity { Id = Guid.NewGuid() };
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        _service.LockIfSupported(entity, userId1);

        // Act
        var result = _service.UnlockIfSupported(entity, userId2);

        // Assert
        Assert.False(result);
        Assert.True(entity.Locking.LockInfo.IsLocked());
    }

    [Fact]
    public void UnlockIfSupported_WithNonLockableEntity_ReturnsTrue()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<EntityLockService<TestEntity>>>();
        var service = new EntityLockService<TestEntity>(loggerMock.Object);
        var entity = new TestEntity { Id = Guid.NewGuid() };
        var userId = Guid.NewGuid();

        // Act
        var result = service.UnlockIfSupported(entity, userId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateLockForUpdate_WithLockedEntityAndCorrectUser_DoesNotThrow()
    {
        // Arrange
        var entity = new TestLockableEntity { Id = Guid.NewGuid() };
        var userId = Guid.NewGuid();
        _service.LockIfSupported(entity, userId);

        // Act & Assert
        _service.ValidateLockForUpdate(entity, userId);
        // Should not throw
    }

    [Fact]
    public void ValidateLockForUpdate_WithUnlockedEntity_ThrowsEntityUnlockedException()
    {
        // Arrange
        var entity = new TestLockableEntity { Id = Guid.NewGuid() };
        var userId = Guid.NewGuid();

        // Act & Assert
        Assert.Throws<EntityUnlockedException>(() => 
            _service.ValidateLockForUpdate(entity, userId));
    }

    [Fact]
    public void ValidateLockForUpdate_WithLockedEntityAndWrongUser_ThrowsEntityLockedException()
    {
        // Arrange
        var entity = new TestLockableEntity { Id = Guid.NewGuid() };
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        _service.LockIfSupported(entity, userId1);

        // Act & Assert
        Assert.Throws<EntityLockedException>(() => 
            _service.ValidateLockForUpdate(entity, userId2));
    }

    [Fact]
    public void ValidateLockForUpdate_WithNonLockableEntity_DoesNotThrow()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<EntityLockService<TestEntity>>>();
        var service = new EntityLockService<TestEntity>(loggerMock.Object);
        var entity = new TestEntity { Id = Guid.NewGuid() };
        var userId = Guid.NewGuid();

        // Act & Assert
        service.ValidateLockForUpdate(entity, userId);
        // Should not throw
    }

    [Fact]
    public void RefreshLockIfOwned_WithLockedEntityAndCorrectUser_RefreshesLock()
    {
        // Arrange
        var entity = new TestLockableEntity { Id = Guid.NewGuid() };
        var userId = Guid.NewGuid();
        _service.LockIfSupported(entity, userId);
        var originalLockTime = entity.Locking.LockInfo.LockedAtUtc;
        
        // Wait a moment to ensure time difference
        Thread.Sleep(10);

        // Act
        _service.RefreshLockIfOwned(entity, userId);

        // Assert
        Assert.True(entity.Locking.LockInfo.IsLocked());
        Assert.True(entity.Locking.LockInfo.LockedAtUtc > originalLockTime);
    }

    [Fact]
    public void RefreshLockIfOwned_WithNonLockableEntity_DoesNothing()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<EntityLockService<TestEntity>>>();
        var service = new EntityLockService<TestEntity>(loggerMock.Object);
        var entity = new TestEntity { Id = Guid.NewGuid() };
        var userId = Guid.NewGuid();

        // Act
        service.RefreshLockIfOwned(entity, userId);

        // Assert
        // Should not throw
        Assert.NotNull(entity);
    }

    [Fact]
    public void IsLockedByAnotherUser_WithLockedEntityAndDifferentUser_ReturnsTrue()
    {
        // Arrange
        var entity = new TestLockableEntity { Id = Guid.NewGuid() };
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        _service.LockIfSupported(entity, userId1);

        // Act
        var result = _service.IsLockedByAnotherUser(entity, userId2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsLockedByAnotherUser_WithLockedEntityAndSameUser_ReturnsFalse()
    {
        // Arrange
        var entity = new TestLockableEntity { Id = Guid.NewGuid() };
        var userId = Guid.NewGuid();
        _service.LockIfSupported(entity, userId);

        // Act
        var result = _service.IsLockedByAnotherUser(entity, userId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsLockedByAnotherUser_WithUnlockedEntity_ReturnsFalse()
    {
        // Arrange
        var entity = new TestLockableEntity { Id = Guid.NewGuid() };
        var userId = Guid.NewGuid();

        // Act
        var result = _service.IsLockedByAnotherUser(entity, userId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ForceUnlockIfSupported_WithLockableEntity_UnlocksEntity()
    {
        // Arrange
        var entity = new TestLockableEntity { Id = Guid.NewGuid() };
        var userId = Guid.NewGuid();
        _service.LockIfSupported(entity, userId);
        Assert.True(entity.Locking.LockInfo.IsLocked());

        // Act
        _service.ForceUnlockIfSupported(entity);

        // Assert
        Assert.False(entity.Locking.LockInfo.IsLocked());
    }

    [Fact]
    public void ForceUnlockIfSupported_WithNonLockableEntity_DoesNothing()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<EntityLockService<TestEntity>>>();
        var service = new EntityLockService<TestEntity>(loggerMock.Object);
        var entity = new TestEntity { Id = Guid.NewGuid() };

        // Act
        service.ForceUnlockIfSupported(entity);

        // Assert
        // Should not throw
        Assert.NotNull(entity);
    }

    [Fact]
    public void LockWithChildrenIfSupported_LocksEntity()
    {
        // Arrange
        var entity = new TestLockableEntity { Id = Guid.NewGuid() };
        var userId = Guid.NewGuid();

        // Act
        _service.LockWithChildrenIfSupported(entity, userId);

        // Assert
        Assert.True(entity.Locking.LockInfo.IsLocked());
        Assert.True(entity.Locking.LockInfo.IsLockedBy(userId));
    }

    [Fact]
    public void UnlockWithChildrenIfSupported_UnlocksEntity()
    {
        // Arrange
        var entity = new TestLockableEntity { Id = Guid.NewGuid() };
        var userId = Guid.NewGuid();
        _service.LockWithChildrenIfSupported(entity, userId);

        // Act
        var result = _service.UnlockWithChildrenIfSupported(entity, userId);

        // Assert
        Assert.True(result);
        Assert.False(entity.Locking.LockInfo.IsLocked());
    }

    [Fact]
    public void RefreshLockWithChildrenIfOwned_RefreshesLock()
    {
        // Arrange
        var entity = new TestLockableEntity { Id = Guid.NewGuid() };
        var userId = Guid.NewGuid();
        _service.LockIfSupported(entity, userId);
        var originalLockTime = entity.Locking.LockInfo.LockedAtUtc;
        
        Thread.Sleep(10);

        // Act
        _service.RefreshLockWithChildrenIfOwned(entity, userId);

        // Assert
        Assert.True(entity.Locking.LockInfo.IsLocked());
        Assert.True(entity.Locking.LockInfo.LockedAtUtc > originalLockTime);
    }

    [Fact]
    public void ForceUnlockWithChildrenIfSupported_UnlocksEntity()
    {
        // Arrange
        var entity = new TestLockableEntity { Id = Guid.NewGuid() };
        var userId = Guid.NewGuid();
        _service.LockIfSupported(entity, userId);

        // Act
        _service.ForceUnlockWithChildrenIfSupported(entity);

        // Assert
        Assert.False(entity.Locking.LockInfo.IsLocked());
    }

    [Fact]
    public void ValidateLockForUpdateWithChildren_WithLockedEntity_DoesNotThrow()
    {
        // Arrange
        var entity = new TestLockableEntity { Id = Guid.NewGuid() };
        var userId = Guid.NewGuid();
        _service.LockIfSupported(entity, userId);

        // Act & Assert
        _service.ValidateLockForUpdateWithChildren(entity, userId);
        // Should not throw
    }

    [Fact]
    public void ValidateLockForUpdateWithChildren_WithUnlockedEntity_ThrowsEntityUnlockedException()
    {
        // Arrange
        var entity = new TestLockableEntity { Id = Guid.NewGuid() };
        var userId = Guid.NewGuid();

        // Act & Assert
        Assert.Throws<EntityUnlockedException>(() => 
            _service.ValidateLockForUpdateWithChildren(entity, userId));
    }
}

