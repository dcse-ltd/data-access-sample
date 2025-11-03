using Infrastructure.Entity.Attributes;
using Infrastructure.Exceptions;
using Infrastructure.Services;
using Infrastructure.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Infrastructure.Tests.Services;

public class EntityLockServiceChildTests
{
    private readonly Mock<ILogger<EntityLockService<TestParentEntity>>> _loggerMock;
    private readonly EntityLockService<TestParentEntity> _service;

    public EntityLockServiceChildTests()
    {
        _loggerMock = new Mock<ILogger<EntityLockService<TestParentEntity>>>();
        _service = new EntityLockService<TestParentEntity>(_loggerMock.Object);
    }

    [Fact]
    public void LockWithChildrenIfSupported_WithChildren_LocksChildren()
    {
        // Arrange
        var parent = new TestParentEntity { Id = Guid.NewGuid(), Name = "Parent" };
        var child1 = new TestLockableEntity { Id = Guid.NewGuid(), Name = "Child1" };
        var child2 = new TestLockableEntity { Id = Guid.NewGuid(), Name = "Child2" };
        parent.Children.Add(child1);
        parent.Children.Add(child2);
        var userId = Guid.NewGuid();

        // Act
        _service.LockWithChildrenIfSupported(parent, userId, maxDepth: 1);

        // Assert
        Assert.True(parent.Locking.LockInfo.IsLocked());
        Assert.True(parent.Locking.LockInfo.IsLockedBy(userId));
        Assert.True(child1.Locking.LockInfo.IsLocked());
        Assert.True(child1.Locking.LockInfo.IsLockedBy(userId));
        Assert.True(child2.Locking.LockInfo.IsLocked());
        Assert.True(child2.Locking.LockInfo.IsLockedBy(userId));
    }

    [Fact]
    public void UnlockWithChildrenIfSupported_WithChildren_UnlocksChildren()
    {
        // Arrange
        var parent = new TestParentEntity { Id = Guid.NewGuid(), Name = "Parent" };
        var child1 = new TestLockableEntity { Id = Guid.NewGuid(), Name = "Child1" };
        var child2 = new TestLockableEntity { Id = Guid.NewGuid(), Name = "Child2" };
        parent.Children.Add(child1);
        parent.Children.Add(child2);
        var userId = Guid.NewGuid();
        _service.LockWithChildrenIfSupported(parent, userId);

        // Act
        var result = _service.UnlockWithChildrenIfSupported(parent, userId, maxDepth: 1);

        // Assert
        Assert.True(result);
        Assert.False(parent.Locking.LockInfo.IsLocked());
        Assert.False(child1.Locking.LockInfo.IsLocked());
        Assert.False(child2.Locking.LockInfo.IsLocked());
    }

    [Fact]
    public void ValidateLockForUpdateWithChildren_WithUnlockedChild_ThrowsEntityUnlockedException()
    {
        // Arrange
        var parent = new TestParentEntity { Id = Guid.NewGuid(), Name = "Parent" };
        var child = new TestLockableEntity { Id = Guid.NewGuid(), Name = "Child" };
        parent.Children.Add(child);
        var userId = Guid.NewGuid();
        _service.LockIfSupported(parent, userId);
        // Child is not locked - we need to manually create a lock service for the child type
        // but since we're testing the parent's validation, the child should trigger the exception

        // Act & Assert
        var exception = Assert.Throws<EntityUnlockedException>(() => 
            _service.ValidateLockForUpdateWithChildren(parent, userId, maxDepth: 1));
        Assert.Equal(child.Id, exception.EntityId);
    }

    [Fact]
    public void ValidateLockForUpdateWithChildren_WithChildLockedByDifferentUser_ThrowsEntityLockedException()
    {
        // Arrange
        var parent = new TestParentEntity { Id = Guid.NewGuid(), Name = "Parent" };
        var child = new TestLockableEntity { Id = Guid.NewGuid(), Name = "Child" };
        parent.Children.Add(child);
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        _service.LockIfSupported(parent, userId1);
        // Lock child using its own lock service
        var childLockService = new EntityLockService<TestLockableEntity>(new Mock<ILogger<EntityLockService<TestLockableEntity>>>().Object);
        childLockService.LockIfSupported(child, userId2); // Child locked by different user

        // Act & Assert
        Assert.Throws<EntityLockedException>(() => 
            _service.ValidateLockForUpdateWithChildren(parent, userId1, maxDepth: 1));
    }

    [Fact]
    public void RefreshLockWithChildrenIfOwned_WithChildren_RefreshesChildrenLocks()
    {
        // Arrange
        var parent = new TestParentEntity { Id = Guid.NewGuid(), Name = "Parent" };
        var child = new TestLockableEntity { Id = Guid.NewGuid(), Name = "Child" };
        parent.Children.Add(child);
        var userId = Guid.NewGuid();
        _service.LockWithChildrenIfSupported(parent, userId);
        var originalParentLockTime = parent.Locking.LockInfo.LockedAtUtc;
        var originalChildLockTime = child.Locking.LockInfo.LockedAtUtc;
        
        Thread.Sleep(10);

        // Act
        _service.RefreshLockWithChildrenIfOwned(parent, userId, maxDepth: 1);

        // Assert
        Assert.True(parent.Locking.LockInfo.LockedAtUtc > originalParentLockTime);
        Assert.True(child.Locking.LockInfo.LockedAtUtc > originalChildLockTime);
    }

    [Fact]
    public void ForceUnlockWithChildrenIfSupported_WithChildren_UnlocksChildren()
    {
        // Arrange
        var parent = new TestParentEntity { Id = Guid.NewGuid(), Name = "Parent" };
        var child = new TestLockableEntity { Id = Guid.NewGuid(), Name = "Child" };
        parent.Children.Add(child);
        var userId = Guid.NewGuid();
        _service.LockWithChildrenIfSupported(parent, userId);
        Assert.True(child.Locking.LockInfo.IsLocked());

        // Act
        _service.ForceUnlockWithChildrenIfSupported(parent, maxDepth: 1);

        // Assert
        Assert.False(parent.Locking.LockInfo.IsLocked());
        Assert.False(child.Locking.LockInfo.IsLocked());
    }

    [Fact]
    public void LockWithChildrenIfSupported_RespectsMaxDepth()
    {
        // Arrange
        var parent = new TestParentEntity { Id = Guid.NewGuid(), Name = "Parent" };
        var child = new TestLockableEntity { Id = Guid.NewGuid(), Name = "Child" };
        parent.Children.Add(child);
        var userId = Guid.NewGuid();

        // Act
        _service.LockWithChildrenIfSupported(parent, userId, maxDepth: 0); // Max depth 0 means no children

        // Assert
        Assert.True(parent.Locking.LockInfo.IsLocked());
        Assert.False(child.Locking.LockInfo.IsLocked()); // Child should not be locked
    }

    [Fact]
    public void ValidateLockForUpdateWithChildren_WithNullChildCollection_DoesNotThrow()
    {
        // Arrange
        var parent = new TestParentEntity { Id = Guid.NewGuid(), Name = "Parent" };
        parent.Children = null!; // Null collection
        var userId = Guid.NewGuid();
        _service.LockIfSupported(parent, userId);

        // Act & Assert
        // Should not throw - null collections are handled
        _service.ValidateLockForUpdateWithChildren(parent, userId, maxDepth: 1);
    }

    [Fact]
    public void LockWithChildrenIfSupported_WithEmptyChildCollection_DoesNotThrow()
    {
        // Arrange
        var parent = new TestParentEntity { Id = Guid.NewGuid(), Name = "Parent" };
        parent.Children.Clear();
        var userId = Guid.NewGuid();

        // Act
        _service.LockWithChildrenIfSupported(parent, userId, maxDepth: 1);

        // Assert
        Assert.True(parent.Locking.LockInfo.IsLocked());
    }

    [Fact]
    public void ValidateLockForUpdateWithChildren_WithExpiredChildLock_ThrowsEntityLockExpiredException()
    {
        // Arrange
        var parent = new TestParentEntity { Id = Guid.NewGuid(), Name = "Parent" };
        var child = new TestLockableEntity { Id = Guid.NewGuid(), Name = "Child" };
        parent.Children.Add(child);
        var userId = Guid.NewGuid();
        _service.LockIfSupported(parent, userId);
        
        // Set child lock to expired
        child.Locking.LockInfo = new Infrastructure.Entity.Models.LockInfo
        {
            LockedByUserId = userId,
            LockedAtUtc = DateTime.UtcNow.AddHours(-2) // Expired
        };

        // Act & Assert
        var exception = Assert.Throws<EntityLockExpiredException>(() => 
            _service.ValidateLockForUpdateWithChildren(parent, userId, maxDepth: 1));
        Assert.Equal(child.Id, exception.EntityId);
    }
}

