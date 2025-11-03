using Infrastructure.Exceptions;

namespace Infrastructure.Tests.Exceptions;

public class EntityNotFoundExceptionTests
{
    [Fact]
    public void Constructor_WithEntityNameAndId_SetsMessage()
    {
        // Arrange
        var entityName = "Customer";
        var entityId = Guid.NewGuid();

        // Act
        var exception = new EntityNotFoundException(entityName, entityId);

        // Assert
        Assert.Contains(entityName, exception.Message);
        Assert.Contains(entityId.ToString(), exception.Message);
    }
}

