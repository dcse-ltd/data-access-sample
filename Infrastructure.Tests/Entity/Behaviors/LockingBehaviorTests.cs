using Infrastructure.Entity.Behaviors;
using Infrastructure.Entity.Interfaces;
using Infrastructure.Exceptions;
using Infrastructure.Tests.Helpers;

namespace Infrastructure.Tests.Entity.Behaviors;

public class LockingBehaviorTests
{
    [Fact]
    public void Lock_WithUnlockedEntity_LocksEntity()
    {
        // Arrange
        var behavior = new LockingBehavior<TestEntity>();
        var userId = Guid.NewGuid();

        // Act
        behavior.Lock(userId);

        // Assert
        Assert.True(behavior.LockInfo.IsLocked());
        Assert.True(behavior.LockInfo.IsLockedBy(userId));
    }

    [Fact]
    public void Lock_WithSameUser_RefreshesLock()
    {
        // Arrange
        var behavior = new LockingBehavior<TestEntity>();
        var userId = Guid.NewGuid();
        behavior.Lock(userId);
        var originalLockTime = behavior.LockInfo.LockedAtUtc;
        
        Thread.Sleep(10);

        // Act
        behavior.Lock(userId);

        // Assert
        Assert.True(behavior.LockInfo.IsLocked());
        Assert.True(behavior.LockInfo.LockedAtUtc > originalLockTime);
    }

    [Fact]
    public void Lock_WithExpiredLock_LocksEntity()
    {
        // Arrange
        var behavior = new LockingBehavior<TestEntity>();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        
        behavior.LockInfo = new Infrastructure.Entity.Models.LockInfo
        {
            LockedByUserId = userId1,
            LockedAtUtc = DateTime.UtcNow.AddHours(-2) // Expired (default timeout is 1 hour)
        };

        // Act
        behavior.Lock(userId2);

        // Assert
        Assert.True(behavior.LockInfo.IsLocked());
        Assert.True(behavior.LockInfo.IsLockedBy(userId2));
    }

    [Fact]
    public void Lock_WithActiveLockByDifferentUser_ThrowsEntityLockedException()
    {
        // Arrange
        var behavior = new LockingBehavior<TestEntity>();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        
        behavior.Lock(userId1);

        // Act & Assert
        var exception = Assert.Throws<EntityLockedException>(() => behavior.Lock(userId2));
        Assert.Equal(userId1, exception.LockedByUserId);
    }

    [Fact]
    public void Unlock_WithCorrectUser_UnlocksEntity()
    {
        // Arrange
        var behavior = new LockingBehavior<TestEntity>();
        var userId = Guid.NewGuid();
        behavior.Lock(userId);

        // Act
        var result = behavior.Unlock(userId);

        // Assert
        Assert.True(result);
        Assert.False(behavior.LockInfo.IsLocked());
    }

    [Fact]
    public void Unlock_WithDifferentUser_ReturnsFalse()
    {
        // Arrange
        var behavior = new LockingBehavior<TestEntity>();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        behavior.Lock(userId1);

        // Act
        var result = behavior.Unlock(userId2);

        // Assert
        Assert.False(result);
        Assert.True(behavior.LockInfo.IsLocked());
    }

    [Fact]
    public void Unlock_WithUnlockedEntity_ReturnsTrue()
    {
        // Arrange
        var behavior = new LockingBehavior<TestEntity>();
        var userId = Guid.NewGuid();

        // Act
        var result = behavior.Unlock(userId);

        // Assert
        Assert.True(result);
        Assert.False(behavior.LockInfo.IsLocked());
    }

    [Fact]
    public void Unlock_WithExpiredLock_UnlocksEntity()
    {
        // Arrange
        var behavior = new LockingBehavior<TestEntity>();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        
        behavior.LockInfo = new Infrastructure.Entity.Models.LockInfo
        {
            LockedByUserId = userId1,
            LockedAtUtc = DateTime.UtcNow.AddHours(-2) // Expired
        };

        // Act
        var result = behavior.Unlock(userId2);

        // Assert
        Assert.True(result);
        Assert.False(behavior.LockInfo.IsLocked());
    }

    [Fact]
    public void UnlockOrThrow_WithCorrectUser_UnlocksEntity()
    {
        // Arrange
        var behavior = new LockingBehavior<TestEntity>();
        var userId = Guid.NewGuid();
        behavior.Lock(userId);

        // Act
        behavior.UnlockOrThrow(userId);

        // Assert
        Assert.False(behavior.LockInfo.IsLocked());
    }

    [Fact]
    public void UnlockOrThrow_WithDifferentUser_ThrowsEntityLockedException()
    {
        // Arrange
        var behavior = new LockingBehavior<TestEntity>();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        behavior.Lock(userId1);

        // Act & Assert
        var exception = Assert.Throws<EntityLockedException>(() => behavior.UnlockOrThrow(userId2));
        Assert.Equal(userId1, exception.LockedByUserId);
    }

    [Fact]
    public void UnlockOrThrow_WithUnlockedEntity_DoesNothing()
    {
        // Arrange
        var behavior = new LockingBehavior<TestEntity>();
        var userId = Guid.NewGuid();

        // Act
        behavior.UnlockOrThrow(userId);

        // Assert
        Assert.False(behavior.LockInfo.IsLocked());
    }

    [Fact]
    public void UnlockOrThrow_WithExpiredLock_UnlocksEntity()
    {
        // Arrange
        var behavior = new LockingBehavior<TestEntity>();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        
        behavior.LockInfo = new Infrastructure.Entity.Models.LockInfo
        {
            LockedByUserId = userId1,
            LockedAtUtc = DateTime.UtcNow.AddHours(-2) // Expired
        };

        // Act
        behavior.UnlockOrThrow(userId2);

        // Assert
        Assert.False(behavior.LockInfo.IsLocked());
    }

    [Fact]
    public void ForceUnlock_UnlocksEntity()
    {
        // Arrange
        var behavior = new LockingBehavior<TestEntity>();
        var userId = Guid.NewGuid();
        behavior.Lock(userId);
        Assert.True(behavior.LockInfo.IsLocked());

        // Act
        behavior.ForceUnlock();

        // Assert
        Assert.False(behavior.LockInfo.IsLocked());
    }

    [Fact]
    public void RefreshLock_WithCorrectUser_RefreshesLock()
    {
        // Arrange
        var behavior = new LockingBehavior<TestEntity>();
        var userId = Guid.NewGuid();
        behavior.Lock(userId);
        var originalLockTime = behavior.LockInfo.LockedAtUtc;
        
        Thread.Sleep(10);

        // Act
        behavior.RefreshLock(userId);

        // Assert
        Assert.True(behavior.LockInfo.IsLocked());
        Assert.True(behavior.LockInfo.LockedAtUtc > originalLockTime);
    }

    [Fact]
    public void RefreshLock_WithDifferentUser_DoesNothing()
    {
        // Arrange
        var behavior = new LockingBehavior<TestEntity>();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        behavior.Lock(userId1);
        var originalLockTime = behavior.LockInfo.LockedAtUtc;
        
        Thread.Sleep(10);

        // Act
        behavior.RefreshLock(userId2);

        // Assert
        Assert.True(behavior.LockInfo.IsLocked());
        Assert.Equal(originalLockTime, behavior.LockInfo.LockedAtUtc);
    }

    [Fact]
    public void LockInfo_Get_ReturnsCopy()
    {
        // Arrange
        var behavior = new LockingBehavior<TestEntity>();
        var userId = Guid.NewGuid();
        behavior.Lock(userId);

        // Act
        var lockInfo1 = behavior.LockInfo;
        var lockInfo2 = behavior.LockInfo;

        // Assert
        Assert.NotSame(lockInfo1, lockInfo2);
        Assert.Equal(lockInfo1.LockedByUserId, lockInfo2.LockedByUserId);
    }

    [Fact]
    public void LockInfo_Set_ThrowsWhenNull()
    {
        // Arrange
        var behavior = new LockingBehavior<TestEntity>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            behavior.LockInfo = null!);
    }
}

