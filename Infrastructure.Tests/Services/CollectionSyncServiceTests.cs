using Infrastructure.Repository.Interfaces;
using Infrastructure.Services;
using Infrastructure.Services.Interfaces;
using Infrastructure.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Infrastructure.Tests.Services;

public class CollectionSyncServiceTests
{
    private readonly Mock<ILogger<CollectionSyncService>> _loggerMock;
    private readonly CollectionSyncService _service;

    public CollectionSyncServiceTests()
    {
        _loggerMock = new Mock<ILogger<CollectionSyncService>>();
        _service = new CollectionSyncService(_loggerMock.Object);
    }

    [Fact]
    public void SyncChildCollection_WithNewDtos_AddsNewEntities()
    {
        // Arrange
        var existingChildren = new List<TestEntity>();
        var dtoChildren = new[]
        {
            new { Id = Guid.NewGuid(), Name = "Child1" },
            new { Id = Guid.NewGuid(), Name = "Child2" }
        };
        var userId = Guid.NewGuid();

        // Act
        _service.SyncChildCollection(
            existingChildren,
            dtoChildren,
            dto => dto.Id,
            (entity, dto) => false,
            (entity, dto) => { },
            dto => new TestEntity { Id = dto.Id, Name = dto.Name },
            null,
            null,
            userId);

        // Assert
        Assert.Equal(2, existingChildren.Count);
        Assert.All(existingChildren, e => Assert.NotEqual(Guid.Empty, e.Id));
    }

    [Fact]
    public void SyncChildCollection_WithRemovedDtos_RemovesEntities()
    {
        // Arrange
        var existingChild1 = new TestEntity { Id = Guid.NewGuid(), Name = "Child1" };
        var existingChild2 = new TestEntity { Id = Guid.NewGuid(), Name = "Child2" };
        var existingChildren = new List<TestEntity> { existingChild1, existingChild2 };
        var dtoChildren = new[]
        {
            new { Id = existingChild1.Id, Name = "Child1" }
        };
        var userId = Guid.NewGuid();

        // Act
        _service.SyncChildCollection(
            existingChildren,
            dtoChildren,
            dto => dto.Id,
            (entity, dto) => false,
            (entity, dto) => { },
            dto => new TestEntity { Id = dto.Id, Name = dto.Name },
            null,
            null,
            userId);

        // Assert
        Assert.Single(existingChildren);
        Assert.Equal(existingChild1.Id, existingChildren[0].Id);
    }

    [Fact]
    public void SyncChildCollection_WithSoftDeletableEntity_SoftDeletesRemovedEntities()
    {
        // Arrange
        var existingChild = new TestSoftDeletableEntity { Id = Guid.NewGuid(), Name = "Child1" };
        var existingChildren = new List<TestSoftDeletableEntity> { existingChild };
        var dtoChildren = Array.Empty<object>();
        var userId = Guid.NewGuid();
        var softDeleteServiceMock = new Mock<IEntitySoftDeleteService<TestSoftDeletableEntity>>();

        // Act
        _service.SyncChildCollection(
            existingChildren,
            dtoChildren,
            dto => Guid.Empty,
            (entity, dto) => false,
            (entity, dto) => { },
            dto => new TestSoftDeletableEntity(),
            softDeleteServiceMock.Object,
            null,
            userId);

        // Assert
        softDeleteServiceMock.Verify(s => s.StampForDelete(existingChild, userId), Times.Once);
    }

    [Fact]
    public void SyncChildCollection_WithChangedEntities_UpdatesEntities()
    {
        // Arrange
        var existingChild = new TestEntity { Id = Guid.NewGuid(), Name = "OldName" };
        var existingChildren = new List<TestEntity> { existingChild };
        var dtoChildren = new[]
        {
            new { Id = existingChild.Id, Name = "NewName" }
        };
        var userId = Guid.NewGuid();
        var updateCalled = false;

        // Act
        _service.SyncChildCollection(
            existingChildren,
            dtoChildren,
            dto => dto.Id,
            (entity, dto) => entity.Name != dto.Name,
            (entity, dto) => { entity.Name = dto.Name; updateCalled = true; },
            dto => new TestEntity { Id = dto.Id, Name = dto.Name },
            null,
            null,
            userId);

        // Assert
        Assert.True(updateCalled);
        Assert.Equal("NewName", existingChild.Name);
    }

    [Fact]
    public void SyncChildCollection_WithUnchangedEntities_SkipsUpdate()
    {
        // Arrange
        var existingChild = new TestEntity { Id = Guid.NewGuid(), Name = "SameName" };
        var existingChildren = new List<TestEntity> { existingChild };
        var dtoChildren = new[]
        {
            new { Id = existingChild.Id, Name = "SameName" }
        };
        var userId = Guid.NewGuid();
        var updateCalled = false;

        // Act
        _service.SyncChildCollection(
            existingChildren,
            dtoChildren,
            dto => dto.Id,
            (entity, dto) => false,
            (entity, dto) => { updateCalled = true; },
            dto => new TestEntity { Id = dto.Id, Name = dto.Name },
            null,
            null,
            userId);

        // Assert
        Assert.False(updateCalled);
    }

    [Fact]
    public void SyncChildCollection_WithEmptyGuid_GeneratesNewId()
    {
        // Arrange
        var existingChildren = new List<TestEntity>();
        var dtoChildren = new[]
        {
            new { Id = Guid.Empty, Name = "NewChild" }
        };
        var userId = Guid.NewGuid();

        // Act
        _service.SyncChildCollection(
            existingChildren,
            dtoChildren,
            dto => dto.Id,
            (entity, dto) => false,
            (entity, dto) => { },
            dto => new TestEntity { Name = dto.Name },
            null,
            null,
            userId);

        // Assert
        Assert.Single(existingChildren);
        Assert.NotEqual(Guid.Empty, existingChildren[0].Id);
    }

    [Fact]
    public void SyncChildCollection_WithRestorableSoftDeletedEntity_RestoresEntity()
    {
        // Arrange
        var existingChild = new TestSoftDeletableEntity 
        { 
            Id = Guid.NewGuid(), 
            Name = "Child1" 
        };
        existingChild.Deleted.MarkSoftDeleted(Guid.NewGuid());
        var existingChildren = new List<TestSoftDeletableEntity> { existingChild };
        var dtoChildren = new[]
        {
            new { Id = existingChild.Id, Name = "Child1" }
        };
        var userId = Guid.NewGuid();
        var softDeleteServiceMock = new Mock<IEntitySoftDeleteService<TestSoftDeletableEntity>>();

        // Act
        _service.SyncChildCollection(
            existingChildren,
            dtoChildren,
            dto => dto.Id,
            (entity, dto) => false,
            (entity, dto) => { },
            dto => new TestSoftDeletableEntity { Id = dto.Id, Name = dto.Name },
            softDeleteServiceMock.Object,
            null,
            userId);

        // Assert
        softDeleteServiceMock.Verify(s => s.StampForRestore(existingChild), Times.Once);
    }
}

