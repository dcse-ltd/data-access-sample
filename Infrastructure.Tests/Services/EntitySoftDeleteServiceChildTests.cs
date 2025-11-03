using Infrastructure.Entity.Attributes;
using Infrastructure.Services;
using Infrastructure.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Infrastructure.Tests.Services;

public class EntitySoftDeleteServiceChildTests
{
    private readonly Mock<ILogger<EntitySoftDeleteService<TestParentSoftDeletableEntity>>> _loggerMock;
    private readonly EntitySoftDeleteService<TestParentSoftDeletableEntity> _service;

    public EntitySoftDeleteServiceChildTests()
    {
        _loggerMock = new Mock<ILogger<EntitySoftDeleteService<TestParentSoftDeletableEntity>>>();
        _service = new EntitySoftDeleteService<TestParentSoftDeletableEntity>(_loggerMock.Object);
    }

    [Fact]
    public void StampForDeleteWithChildren_WithChildren_SoftDeletesChildren()
    {
        // Arrange
        var parent = new TestParentSoftDeletableEntity { Id = Guid.NewGuid(), Name = "Parent" };
        var child1 = new TestSoftDeletableEntity { Id = Guid.NewGuid(), Name = "Child1" };
        var child2 = new TestSoftDeletableEntity { Id = Guid.NewGuid(), Name = "Child2" };
        parent.Children.Add(child1);
        parent.Children.Add(child2);
        var userId = Guid.NewGuid();

        // Act
        _service.StampForDeleteWithChildren(parent, userId, maxDepth: 1);

        // Assert
        Assert.True(parent.Deleted.SoftDeleteInfo.IsDeleted);
        Assert.Equal(userId, parent.Deleted.SoftDeleteInfo.DeletedByUserId);
        Assert.True(child1.Deleted.SoftDeleteInfo.IsDeleted);
        Assert.Equal(userId, child1.Deleted.SoftDeleteInfo.DeletedByUserId);
        Assert.True(child2.Deleted.SoftDeleteInfo.IsDeleted);
        Assert.Equal(userId, child2.Deleted.SoftDeleteInfo.DeletedByUserId);
    }

    [Fact]
    public void StampForRestoreWithChildren_WithChildren_RestoresChildren()
    {
        // Arrange
        var parent = new TestParentSoftDeletableEntity { Id = Guid.NewGuid(), Name = "Parent" };
        var child1 = new TestSoftDeletableEntity { Id = Guid.NewGuid(), Name = "Child1" };
        var child2 = new TestSoftDeletableEntity { Id = Guid.NewGuid(), Name = "Child2" };
        parent.Children.Add(child1);
        parent.Children.Add(child2);
        var userId = Guid.NewGuid();
        _service.StampForDeleteWithChildren(parent, userId);
        Assert.True(parent.Deleted.SoftDeleteInfo.IsDeleted);
        Assert.True(child1.Deleted.SoftDeleteInfo.IsDeleted);

        // Act
        _service.StampForRestoreWithChildren(parent, maxDepth: 1);

        // Assert
        Assert.False(parent.Deleted.SoftDeleteInfo.IsDeleted);
        Assert.False(child1.Deleted.SoftDeleteInfo.IsDeleted);
        Assert.False(child2.Deleted.SoftDeleteInfo.IsDeleted);
    }

    [Fact]
    public void StampForDeleteWithChildren_RespectsMaxDepth()
    {
        // Arrange
        var parent = new TestParentSoftDeletableEntity { Id = Guid.NewGuid(), Name = "Parent" };
        var child = new TestSoftDeletableEntity { Id = Guid.NewGuid(), Name = "Child" };
        parent.Children.Add(child);
        var userId = Guid.NewGuid();

        // Act
        _service.StampForDeleteWithChildren(parent, userId, maxDepth: 0); // Max depth 0 means no children

        // Assert
        Assert.True(parent.Deleted.SoftDeleteInfo.IsDeleted);
        Assert.False(child.Deleted.SoftDeleteInfo.IsDeleted); // Child should not be soft deleted
    }

    [Fact]
    public void StampForDeleteWithChildren_WithNullChildCollection_DoesNotThrow()
    {
        // Arrange
        var parent = new TestParentSoftDeletableEntity { Id = Guid.NewGuid(), Name = "Parent" };
        parent.Children = null!; // Null collection
        var userId = Guid.NewGuid();

        // Act
        _service.StampForDeleteWithChildren(parent, userId, maxDepth: 1);

        // Assert
        Assert.True(parent.Deleted.SoftDeleteInfo.IsDeleted);
    }

    [Fact]
    public void StampForDeleteWithChildren_WithEmptyChildCollection_DoesNotThrow()
    {
        // Arrange
        var parent = new TestParentSoftDeletableEntity { Id = Guid.NewGuid(), Name = "Parent" };
        parent.Children.Clear();
        var userId = Guid.NewGuid();

        // Act
        _service.StampForDeleteWithChildren(parent, userId, maxDepth: 1);

        // Assert
        Assert.True(parent.Deleted.SoftDeleteInfo.IsDeleted);
    }

    [Fact]
    public void StampForRestoreWithChildren_WithNonSoftDeletableChild_OnlyRestoresSoftDeletable()
    {
        // Arrange
        var parent = new TestParentSoftDeletableEntity { Id = Guid.NewGuid(), Name = "Parent" };
        var child = new TestSoftDeletableEntity { Id = Guid.NewGuid(), Name = "Child" };
        parent.Children.Add(child);
        var userId = Guid.NewGuid();
        _service.StampForDeleteWithChildren(parent, userId);

        // Act
        _service.StampForRestoreWithChildren(parent, maxDepth: 1);

        // Assert
        Assert.False(parent.Deleted.SoftDeleteInfo.IsDeleted);
        Assert.False(child.Deleted.SoftDeleteInfo.IsDeleted);
    }
}

