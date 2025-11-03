using Infrastructure.Repository.Models;

namespace Infrastructure.Tests.Repository.Models;

public class PagedResultTests
{
    [Fact]
    public void Constructor_WithValidParameters_SetsProperties()
    {
        // Arrange
        var items = new[] { "item1", "item2", "item3" };
        var totalCount = 10;
        var page = 1;
        var pageSize = 3;

        // Act
        var result = new PagedResult<string>(items, totalCount, page, pageSize);

        // Assert
        Assert.Equal(items, result.Items);
        Assert.Equal(totalCount, result.TotalCount);
        Assert.Equal(page, result.Page);
        Assert.Equal(pageSize, result.PageSize);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithInvalidPage_ThrowsArgumentOutOfRangeException(int invalidPage)
    {
        // Arrange
        var items = new[] { "item1" };
        var totalCount = 10;
        var pageSize = 3;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            new PagedResult<string>(items, totalCount, invalidPage, pageSize));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithInvalidPageSize_ThrowsArgumentOutOfRangeException(int invalidPageSize)
    {
        // Arrange
        var items = new[] { "item1" };
        var totalCount = 10;
        var page = 1;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            new PagedResult<string>(items, totalCount, page, invalidPageSize));
    }

    [Theory]
    [InlineData(10, 3, 4)] // 10 items, 3 per page = 4 pages
    [InlineData(9, 3, 3)]  // 9 items, 3 per page = 3 pages
    [InlineData(10, 4, 3)] // 10 items, 4 per page = 3 pages (ceiling)
    [InlineData(0, 10, 0)] // 0 items = 0 pages
    public void TotalPages_CalculatesCorrectly(int totalCount, int pageSize, int expectedPages)
    {
        // Arrange
        var items = Enumerable.Range(1, Math.Min(pageSize, totalCount)).Select(i => i.ToString()).ToArray();
        var page = 1;
        var result = new PagedResult<string>(items, totalCount, page, pageSize);

        // Act
        var totalPages = result.TotalPages;

        // Assert
        Assert.Equal(expectedPages, totalPages);
    }

    [Theory]
    [InlineData(1, false)] // First page
    [InlineData(2, true)]   // Not first page
    [InlineData(5, true)]   // Middle page
    public void HasPreviousPage_ReturnsCorrectValue(int page, bool expected)
    {
        // Arrange
        var items = new[] { "item1" };
        var totalCount = 10;
        var pageSize = 3;
        var result = new PagedResult<string>(items, totalCount, page, pageSize);

        // Act
        var hasPrevious = result.HasPreviousPage;

        // Assert
        Assert.Equal(expected, hasPrevious);
    }

    [Theory]
    [InlineData(1, 10, 3, true)]   // First page, more pages exist
    [InlineData(4, 10, 3, false)]  // Last page
    [InlineData(3, 9, 3, false)]   // Last page (exact match)
    [InlineData(1, 2, 3, false)]   // Only one page
    public void HasNextPage_ReturnsCorrectValue(int page, int totalCount, int pageSize, bool expected)
    {
        // Arrange
        var items = Enumerable.Range(1, Math.Min(pageSize, totalCount)).Select(i => i.ToString()).ToArray();
        var result = new PagedResult<string>(items, totalCount, page, pageSize);

        // Act
        var hasNext = result.HasNextPage;

        // Assert
        Assert.Equal(expected, hasNext);
    }

    [Fact]
    public void Items_CanBeEmpty()
    {
        // Arrange
        var items = Array.Empty<string>();
        var totalCount = 0;
        var page = 1;
        var pageSize = 10;

        // Act
        var result = new PagedResult<string>(items, totalCount, page, pageSize);

        // Assert
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }
}

