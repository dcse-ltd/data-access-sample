using Infrastructure.Exceptions;

namespace Infrastructure.Tests.Exceptions;

public class EntityLockedExceptionTests
{
    [Fact]
    public void Constructor_WithEntityNameAndUserId_SetsMessage()
    {
        // Arrange
        var entityName = "Customer";
        var userId = Guid.NewGuid();
        var lockedAt = DateTime.UtcNow;

        // Act
        var exception = new EntityLockedException(entityName, userId, lockedAt);

        // Assert
        Assert.Contains(entityName, exception.Message);
        Assert.Contains(userId.ToString(), exception.Message);
    }

    [Fact]
    public void Constructor_WithNullLockedAt_HandlesNull()
    {
        // Arrange
        var entityName = "Customer";
        var userId = Guid.NewGuid();

        // Act
        var exception = new EntityLockedException(entityName, userId, null);

        // Assert
        Assert.Contains(entityName, exception.Message);
        Assert.Contains(userId.ToString(), exception.Message);
    }
}

