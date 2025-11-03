using Infrastructure.Entity.Models;

namespace Infrastructure.Tests.Entity.Models;

public class SoftDeleteInfoTests
{
    [Fact]
    public void IsDeletedBy_WhenDeletedBySameUser_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var softDeleteInfo = new SoftDeleteInfo
        {
            IsDeleted = true,
            DeletedByUserId = userId,
            DeletedAtUtc = DateTime.UtcNow
        };

        // Act
        var result = softDeleteInfo.IsDeletedBy(userId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsDeletedBy_WhenDeletedByDifferentUser_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var softDeleteInfo = new SoftDeleteInfo
        {
            IsDeleted = true,
            DeletedByUserId = userId,
            DeletedAtUtc = DateTime.UtcNow
        };

        // Act
        var result = softDeleteInfo.IsDeletedBy(otherUserId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsDeletedBy_WhenNotDeleted_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var softDeleteInfo = new SoftDeleteInfo
        {
            IsDeleted = false,
            DeletedByUserId = userId
        };

        // Act
        var result = softDeleteInfo.IsDeletedBy(userId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsDeletedLongerThan_WhenDeletedLongerThanDuration_ReturnsTrue()
    {
        // Arrange
        var softDeleteInfo = new SoftDeleteInfo
        {
            IsDeleted = true,
            DeletedAtUtc = DateTime.UtcNow.AddDays(-2), // Deleted 2 days ago
            DeletedByUserId = Guid.NewGuid()
        };
        var duration = TimeSpan.FromDays(1);

        // Act
        var result = softDeleteInfo.IsDeletedLongerThan(duration);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsDeletedLongerThan_WhenDeletedShorterThanDuration_ReturnsFalse()
    {
        // Arrange
        var softDeleteInfo = new SoftDeleteInfo
        {
            IsDeleted = true,
            DeletedAtUtc = DateTime.UtcNow.AddHours(-12), // Deleted 12 hours ago
            DeletedByUserId = Guid.NewGuid()
        };
        var duration = TimeSpan.FromDays(1);

        // Act
        var result = softDeleteInfo.IsDeletedLongerThan(duration);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsDeletedLongerThan_WhenNotDeleted_ReturnsFalse()
    {
        // Arrange
        var softDeleteInfo = new SoftDeleteInfo
        {
            IsDeleted = false,
            DeletedAtUtc = DateTime.UtcNow.AddDays(-2)
        };
        var duration = TimeSpan.FromDays(1);

        // Act
        var result = softDeleteInfo.IsDeletedLongerThan(duration);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsDeletedLongerThan_WhenDeletedAtUtcIsNull_ReturnsFalse()
    {
        // Arrange
        var softDeleteInfo = new SoftDeleteInfo
        {
            IsDeleted = true,
            DeletedAtUtc = null
        };
        var duration = TimeSpan.FromDays(1);

        // Act
        var result = softDeleteInfo.IsDeletedLongerThan(duration);

        // Assert
        Assert.False(result);
    }
}

