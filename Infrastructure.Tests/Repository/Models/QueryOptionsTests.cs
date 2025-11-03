using Infrastructure.Repository.Models;

namespace Infrastructure.Tests.Repository.Models;

public class QueryOptionsTests
{
    [Fact]
    public void Constructor_WithDefaultParameters_CreatesDefaultOptions()
    {
        // Act
        var options = new QueryOptions();

        // Assert
        Assert.False(options.TrackChanges);
        Assert.False(options.IncludeSoftDeleted);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void Constructor_WithParameters_SetsProperties(bool trackChanges, bool includeSoftDeleted)
    {
        // Act
        var options = new QueryOptions(trackChanges, includeSoftDeleted);

        // Assert
        Assert.Equal(trackChanges, options.TrackChanges);
        Assert.Equal(includeSoftDeleted, options.IncludeSoftDeleted);
    }

    [Fact]
    public void Default_ReturnsCorrectOptions()
    {
        // Act
        var options = QueryOptions.Default;

        // Assert
        Assert.False(options.TrackChanges);
        Assert.False(options.IncludeSoftDeleted);
    }

    [Fact]
    public void Tracking_ReturnsCorrectOptions()
    {
        // Act
        var options = QueryOptions.Tracking;

        // Assert
        Assert.True(options.TrackChanges);
        Assert.False(options.IncludeSoftDeleted);
    }

    [Fact]
    public void SoftDeleted_ReturnsCorrectOptions()
    {
        // Act
        var options = QueryOptions.SoftDeleted;

        // Assert
        Assert.False(options.TrackChanges);
        Assert.True(options.IncludeSoftDeleted);
    }

    [Fact]
    public void All_ReturnsCorrectOptions()
    {
        // Act
        var options = QueryOptions.All;

        // Assert
        Assert.False(options.TrackChanges);
        Assert.True(options.IncludeSoftDeleted);
    }

    [Fact]
    public void ForRestore_ReturnsCorrectOptions()
    {
        // Act
        var options = QueryOptions.ForRestore;

        // Assert
        Assert.True(options.TrackChanges);
        Assert.True(options.IncludeSoftDeleted);
    }

    [Fact]
    public void RecordEquality_WithSameValues_ReturnsTrue()
    {
        // Arrange
        var options1 = new QueryOptions(true, false);
        var options2 = new QueryOptions(true, false);

        // Act & Assert
        Assert.Equal(options1, options2);
    }

    [Fact]
    public void RecordEquality_WithDifferentValues_ReturnsFalse()
    {
        // Arrange
        var options1 = new QueryOptions(true, false);
        var options2 = new QueryOptions(false, true);

        // Act & Assert
        Assert.NotEqual(options1, options2);
    }
}

