using Infrastructure.Exceptions;

namespace Infrastructure.Tests.Exceptions;

public class ConcurrencyExceptionTests
{
    [Fact]
    public void Constructor_WithEntityName_SetsMessage()
    {
        // Arrange
        var entityName = "Customer";
        var entityId = Guid.NewGuid();
        byte[]? clientRowVersion = null;
        byte[]? databaseRowVersion = null;

        // Act
        var exception = new ConcurrencyException(entityName, entityId, clientRowVersion, databaseRowVersion);

        // Assert
        Assert.Contains(entityName, exception.Message);
        Assert.Contains(entityId.ToString(), exception.Message);
        Assert.Equal(entityName, exception.EntityType);
        Assert.Equal(entityId, exception.EntityId);
    }

    [Fact]
    public void Constructor_WithRowVersions_SetsProperties()
    {
        // Arrange
        var entityName = "Customer";
        var entityId = Guid.NewGuid();
        var clientRowVersion = new byte[] { 1, 2, 3 };
        var databaseRowVersion = new byte[] { 4, 5, 6 };

        // Act
        var exception = new ConcurrencyException(entityName, entityId, clientRowVersion, databaseRowVersion);

        // Assert
        Assert.Equal(clientRowVersion, exception.ClientRowVersion);
        Assert.Equal(databaseRowVersion, exception.DatabaseRowVersion);
    }
}

