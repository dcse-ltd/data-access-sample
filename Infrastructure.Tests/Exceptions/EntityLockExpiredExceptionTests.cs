using Infrastructure.Exceptions;

namespace Infrastructure.Tests.Exceptions;

public class EntityLockExpiredExceptionTests
{
    [Fact]
    public void Constructor_WithAllParameters_SetsMessage()
    {
        // Arrange
        var entityName = "Customer";
        var entityId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var lockedAt = DateTime.UtcNow;
        var timeoutMinutes = 15;

        // Act
        var exception = new EntityLockExpiredException(entityName, entityId, userId, lockedAt, timeoutMinutes);

        // Assert
        Assert.Contains(entityName, exception.Message);
        Assert.Contains(entityId.ToString(), exception.Message);
        Assert.Contains(userId.ToString(), exception.Message);
    }
}

