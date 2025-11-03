using Infrastructure.Exceptions;
using Infrastructure.Services;
using Infrastructure.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Infrastructure.Tests.Services;

public class ConcurrencyServiceTests
{
    private readonly Mock<ILogger<ConcurrencyService<TestConcurrencyEntity>>> _loggerMock;
    private readonly ConcurrencyService<TestConcurrencyEntity> _service;

    public ConcurrencyServiceTests()
    {
        _loggerMock = new Mock<ILogger<ConcurrencyService<TestConcurrencyEntity>>>();
        _service = new ConcurrencyService<TestConcurrencyEntity>(_loggerMock.Object);
    }

    [Fact]
    public void HandleConcurrencyException_WithNonConcurrencyEntity_ThrowsOriginalException()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<ConcurrencyService<TestEntity>>>();
        var service = new ConcurrencyService<TestEntity>(loggerMock.Object);
        var entity = new TestEntity { Id = Guid.NewGuid() };
        var exception = new DbUpdateConcurrencyException("Concurrency conflict", (Exception?)null, Array.Empty<Microsoft.EntityFrameworkCore.Update.IUpdateEntry>());

        // Act & Assert
        Assert.Throws<DbUpdateConcurrencyException>(() => 
            service.HandleConcurrencyException(entity, exception));
    }

    [Fact]
    public void HandleConcurrencyException_WithEmptyEntries_ThrowsOriginalException()
    {
        // Arrange
        var entity = new TestConcurrencyEntity { Id = Guid.NewGuid() };
        var exception = new DbUpdateConcurrencyException("Concurrency conflict", (Exception?)null, Array.Empty<Microsoft.EntityFrameworkCore.Update.IUpdateEntry>());

        // Act & Assert
        Assert.Throws<DbUpdateConcurrencyException>(() => 
            _service.HandleConcurrencyException(entity, exception));
    }
}

