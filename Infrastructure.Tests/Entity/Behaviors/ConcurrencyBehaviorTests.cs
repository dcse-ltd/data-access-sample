namespace Infrastructure.Tests.Entity.Behaviors;

public class ConcurrencyBehaviorTests
{
    [Fact]
    public void RowVersion_Get_ReturnsInitializedArray()
    {
        // Arrange & Act
        var behavior = new Infrastructure.Entity.Behaviors.ConcurrencyBehavior();

        // Assert
        Assert.NotNull(behavior.RowVersion);
        Assert.Empty(behavior.RowVersion);
    }

    [Fact]
    public void RowVersion_Set_StoresValue()
    {
        // Arrange
        var behavior = new Infrastructure.Entity.Behaviors.ConcurrencyBehavior();
        var rowVersion = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        behavior.RowVersion = rowVersion;

        // Assert
        Assert.Equal(rowVersion, behavior.RowVersion);
    }
}

