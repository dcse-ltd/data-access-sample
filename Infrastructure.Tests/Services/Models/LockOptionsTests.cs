using Infrastructure.Services.Models;

namespace Infrastructure.Tests.Services.Models;

public class LockOptionsTests
{
    [Fact]
    public void Constructor_InitializesWithDefaultValues()
    {
        // Act
        var options = new LockOptions();

        // Assert
        Assert.False(options.IncludeChildren);
        Assert.Equal(1, options.MaxDepth);
    }

    [Fact]
    public void IncludeChildren_CanBeSet()
    {
        // Arrange
        var options = new LockOptions();

        // Act
        options.IncludeChildren = true;

        // Assert
        Assert.True(options.IncludeChildren);
    }

    [Fact]
    public void MaxDepth_WithValidValue_SetsProperty()
    {
        // Arrange
        var options = new LockOptions();

        // Act
        options.MaxDepth = 5;

        // Assert
        Assert.Equal(5, options.MaxDepth);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void MaxDepth_WithInvalidValue_ThrowsArgumentOutOfRangeException(int invalidDepth)
    {
        // Arrange
        var options = new LockOptions();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => options.MaxDepth = invalidDepth);
    }

    [Fact]
    public void MaxDepth_WithPositiveValue_Works()
    {
        // Arrange
        var options = new LockOptions();

        // Act
        options.MaxDepth = 10;

        // Assert
        Assert.Equal(10, options.MaxDepth);
    }
}

