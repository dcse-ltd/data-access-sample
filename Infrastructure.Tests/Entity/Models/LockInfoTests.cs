using Infrastructure.Entity.Models;

namespace Infrastructure.Tests.Entity.Models;

public class LockInfoTests
{
    [Fact]
    public void IsExpired_WhenNotLocked_ReturnsFalse()
    {
        // Arrange
        var lockInfo = new LockInfo
        {
            LockedByUserId = null,
            LockedAtUtc = null
        };

        // Act
        var result = lockInfo.IsExpired();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsExpired_WhenLockedAndNotExpired_ReturnsFalse()
    {
        // Arrange
        var lockInfo = new LockInfo
        {
            LockedByUserId = Guid.NewGuid(),
            LockedAtUtc = DateTime.UtcNow.AddMinutes(-5), // 5 minutes ago
            LockTimeoutMinutes = 15
        };

        // Act
        var result = lockInfo.IsExpired();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsExpired_WhenLockedAndExpired_ReturnsTrue()
    {
        // Arrange
        var lockInfo = new LockInfo
        {
            LockedByUserId = Guid.NewGuid(),
            LockedAtUtc = DateTime.UtcNow.AddMinutes(-20), // 20 minutes ago
            LockTimeoutMinutes = 15
        };

        // Act
        var result = lockInfo.IsExpired();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsLockedBy_WhenLockedBySameUser_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var lockInfo = new LockInfo
        {
            LockedByUserId = userId,
            LockedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            LockTimeoutMinutes = 15
        };

        // Act
        var result = lockInfo.IsLockedBy(userId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsLockedBy_WhenLockedByDifferentUser_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var lockInfo = new LockInfo
        {
            LockedByUserId = userId,
            LockedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            LockTimeoutMinutes = 15
        };

        // Act
        var result = lockInfo.IsLockedBy(otherUserId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsLockedBy_WhenExpired_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var lockInfo = new LockInfo
        {
            LockedByUserId = userId,
            LockedAtUtc = DateTime.UtcNow.AddMinutes(-20),
            LockTimeoutMinutes = 15
        };

        // Act
        var result = lockInfo.IsLockedBy(userId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsLocked_WhenNotLocked_ReturnsFalse()
    {
        // Arrange
        var lockInfo = new LockInfo
        {
            LockedByUserId = null,
            LockedAtUtc = null
        };

        // Act
        var result = lockInfo.IsLocked();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsLocked_WhenLockedAndNotExpired_ReturnsTrue()
    {
        // Arrange
        var lockInfo = new LockInfo
        {
            LockedByUserId = Guid.NewGuid(),
            LockedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            LockTimeoutMinutes = 15
        };

        // Act
        var result = lockInfo.IsLocked();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsLocked_WhenExpired_ReturnsFalse()
    {
        // Arrange
        var lockInfo = new LockInfo
        {
            LockedByUserId = Guid.NewGuid(),
            LockedAtUtc = DateTime.UtcNow.AddMinutes(-20),
            LockTimeoutMinutes = 15
        };

        // Act
        var result = lockInfo.IsLocked();

        // Assert
        Assert.False(result);
    }
}

