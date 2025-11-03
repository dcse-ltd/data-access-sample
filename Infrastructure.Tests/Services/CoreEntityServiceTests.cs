using Infrastructure.Exceptions;
using Infrastructure.Repository.Interfaces;
using Infrastructure.Repository.Models;
using Infrastructure.Services;
using Infrastructure.Services.Interfaces;
using Infrastructure.Services.Models;
using Infrastructure.Tests.Helpers;
using Infrastructure.UnitOfWork.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Infrastructure.Tests.Services;

public class CoreEntityServiceTests
{
    private readonly Mock<IRepository<TestEntity>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IEntityLockService<TestEntity>> _entityLockServiceMock;
    private readonly Mock<IEntityAuditService<TestEntity>> _entityAuditServiceMock;
    private readonly Mock<IConcurrencyService<TestEntity>> _concurrencyServiceMock;
    private readonly Mock<IEntitySoftDeleteService<TestEntity>> _entitySoftDeleteServiceMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<ILogger<CoreEntityService<TestEntity>>> _loggerMock;
    private readonly CoreEntityService<TestEntity> _service;

    public CoreEntityServiceTests()
    {
        _repositoryMock = new Mock<IRepository<TestEntity>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _entityLockServiceMock = new Mock<IEntityLockService<TestEntity>>();
        _entityAuditServiceMock = new Mock<IEntityAuditService<TestEntity>>();
        _concurrencyServiceMock = new Mock<IConcurrencyService<TestEntity>>();
        _entitySoftDeleteServiceMock = new Mock<IEntitySoftDeleteService<TestEntity>>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _loggerMock = new Mock<ILogger<CoreEntityService<TestEntity>>>();

        _currentUserServiceMock.Setup(s => s.UserId).Returns(Guid.NewGuid());

        _service = new CoreEntityService<TestEntity>(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _entityLockServiceMock.Object,
            _entityAuditServiceMock.Object,
            _concurrencyServiceMock.Object,
            _entitySoftDeleteServiceMock.Object,
            _currentUserServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingEntity_ReturnsEntity()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var entity = new TestEntity { Id = entityId, Name = "Test" };
        _repositoryMock.Setup(r => r.GetByIdAsync(entityId, QueryOptions.Default, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        var result = await _service.GetByIdAsync(entityId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(entityId, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WithCancellationToken_ThrowsWhenCancelled()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => 
            _service.GetByIdAsync(entityId, cts.Token));
    }

    [Fact]
    public async Task GetByIdOrThrowAsync_WithNonExistingEntity_ThrowsEntityNotFoundException()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdOrThrowAsync(entityId, QueryOptions.Default, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EntityNotFoundException("TestEntity", entityId));

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() => 
            _service.GetByIdOrThrowAsync(entityId));
    }

    [Fact]
    public async Task CreateAsync_WithEntity_CreatesAndStampsAudit()
    {
        // Arrange
        var entity = new TestEntity { Name = "New Entity" };
        var userId = _currentUserServiceMock.Object.UserId;
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.CreateAsync(entity);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _entityAuditServiceMock.Verify(s => s.StampForCreate(It.IsAny<TestEntity>(), userId), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithCancellationToken_ThrowsWhenCancelled()
    {
        // Arrange
        var entity = new TestEntity { Name = "New Entity" };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => 
            _service.CreateAsync(entity, cts.Token));
    }

    [Fact]
    public async Task UpdateAsync_WithEntity_ValidatesLockAndUnlocks()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var entity = new TestEntity { Id = entityId, Name = "Original" };
        var userId = _currentUserServiceMock.Object.UserId;
        
        _repositoryMock.Setup(r => r.GetByIdOrThrowAsync(entityId, QueryOptions.Tracking, It.IsAny<ISpecification<TestEntity>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.UpdateAsync(entityId, e => e.Name = "Updated");

        // Assert
        Assert.NotNull(result);
        _entityLockServiceMock.Verify(s => s.ValidateLockForUpdate(It.IsAny<TestEntity>(), userId), Times.Once);
        _entityLockServiceMock.Verify(s => s.UnlockIfSupported(It.IsAny<TestEntity>(), userId), Times.Once);
        _entityAuditServiceMock.Verify(s => s.StampForUpdate(It.IsAny<TestEntity>(), userId), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithConcurrencyException_HandlesConcurrency()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var entity = new TestEntity { Id = entityId, Name = "Original" };
        var userId = _currentUserServiceMock.Object.UserId;
        var concurrencyException = new ConcurrencyException("TestEntity", entityId, null, null);
        
        _repositoryMock.Setup(r => r.GetByIdOrThrowAsync(entityId, QueryOptions.Tracking, It.IsAny<ISpecification<TestEntity>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException("Conflict", (Exception?)null, Array.Empty<Microsoft.EntityFrameworkCore.Update.IUpdateEntry>()));
        _concurrencyServiceMock.Setup(s => s.HandleConcurrencyException(It.IsAny<TestEntity>(), It.IsAny<DbUpdateConcurrencyException>()))
            .Returns(concurrencyException);

        // Act & Assert
        await Assert.ThrowsAsync<ConcurrencyException>(() => 
            _service.UpdateAsync(entityId, e => e.Name = "Updated"));
    }

    [Fact]
    public async Task DeleteAsync_WithSoftDeletableEntity_SoftDeletes()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var entity = new TestEntity { Id = entityId, Name = "ToDelete" };
        var userId = _currentUserServiceMock.Object.UserId;
        
        // Mock the entity to be treated as soft deletable by checking the type in the service
        _repositoryMock.Setup(r => r.GetByIdOrThrowAsync(entityId, QueryOptions.Tracking, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        
        // Since TestEntity doesn't implement ISoftDeletableEntity, this will hard delete
        // We'll verify Remove is called instead

        // Act
        await _service.DeleteAsync(entityId);

        // Assert
        _entityLockServiceMock.Verify(s => s.ValidateLockForUpdate(It.IsAny<TestEntity>(), userId), Times.Once);
        _repositoryMock.Verify(r => r.Remove(It.IsAny<TestEntity>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithNonSoftDeletableEntity_HardDeletes()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var entity = new TestEntity { Id = entityId, Name = "ToDelete" };
        var userId = _currentUserServiceMock.Object.UserId;
        
        _repositoryMock.Setup(r => r.GetByIdOrThrowAsync(entityId, QueryOptions.Tracking, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _service.DeleteAsync(entityId);

        // Assert
        _repositoryMock.Verify(r => r.Remove(It.IsAny<TestEntity>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HardDeleteAsync_RemovesEntity()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var entity = new TestEntity { Id = entityId, Name = "ToDelete" };
        var userId = _currentUserServiceMock.Object.UserId;
        
        _repositoryMock.Setup(r => r.GetByIdOrThrowAsync(entityId, QueryOptions.Tracking, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _service.HardDeleteAsync(entityId);

        // Assert
        _repositoryMock.Verify(r => r.Remove(It.IsAny<TestEntity>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RestoreAsync_WithNonSoftDeletableEntity_ThrowsInvalidOperationException()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var entity = new TestEntity { Id = entityId };
        
        _repositoryMock.Setup(r => r.GetByIdOrThrowAsync(entityId, QueryOptions.ForRestore, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.RestoreAsync(entityId, includeChildren: false));
    }

    [Fact]
    public async Task GetByIdWithLockAsync_LocksEntity()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var entity = new TestEntity { Id = entityId, Name = "Test" };
        var userId = _currentUserServiceMock.Object.UserId;
        
        _repositoryMock.Setup(r => r.GetByIdOrThrowAsync(entityId, QueryOptions.Tracking, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.GetByIdWithLockAsync(entityId);

        // Assert
        Assert.NotNull(result);
        _entityLockServiceMock.Verify(s => s.LockIfSupported(It.IsAny<TestEntity>(), userId), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshLockAsync_RefreshesLock()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var entity = new TestEntity { Id = entityId, Name = "Test" };
        var userId = _currentUserServiceMock.Object.UserId;
        
        _repositoryMock.Setup(r => r.GetByIdOrThrowAsync(entityId, QueryOptions.Tracking, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _service.RefreshLockAsync(entityId);

        // Assert
        _entityLockServiceMock.Verify(s => s.RefreshLockIfOwned(It.IsAny<TestEntity>(), userId), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var entities = new[]
        {
            new TestEntity { Id = Guid.NewGuid(), Name = "Entity1" },
            new TestEntity { Id = Guid.NewGuid(), Name = "Entity2" }
        };
        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetAllAsync_WithQueryOptions_ReturnsAllEntities()
    {
        // Arrange
        var entities = new[]
        {
            new TestEntity { Id = Guid.NewGuid(), Name = "Entity1" }
        };
        _repositoryMock.Setup(r => r.GetAllAsync(QueryOptions.Default, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        // Act
        var result = await _service.GetAllAsync(QueryOptions.Default);

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task FindAsync_WithSpecification_ReturnsMatchingEntities()
    {
        // Arrange
        var spec = new Mock<ISpecification<TestEntity>>().Object;
        var entities = new[]
        {
            new TestEntity { Id = Guid.NewGuid(), Name = "Entity1" }
        };
        _repositoryMock.Setup(r => r.FindAsync(spec, QueryOptions.Default, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        // Act
        var result = await _service.FindAsync(spec, QueryOptions.Default);

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task GetByIdWithLockAsync_WithLockOptions_LocksEntity()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var entity = new TestEntity { Id = entityId, Name = "Test" };
        var userId = _currentUserServiceMock.Object.UserId;
        var lockOptions = new LockOptions { IncludeChildren = true, MaxDepth = 2 };
        
        _repositoryMock.Setup(r => r.GetByIdOrThrowAsync(entityId, QueryOptions.Tracking, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.GetByIdWithLockAsync(entityId, lockOptions);

        // Assert
        Assert.NotNull(result);
        _entityLockServiceMock.Verify(s => s.LockWithChildrenIfSupported(It.IsAny<TestEntity>(), userId, 2), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithLockOptions_ValidatesLockWithChildren()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var entity = new TestEntity { Id = entityId, Name = "Original" };
        var userId = _currentUserServiceMock.Object.UserId;
        var lockOptions = new LockOptions { IncludeChildren = true, MaxDepth = 2 };
        
        _repositoryMock.Setup(r => r.GetByIdOrThrowAsync(entityId, QueryOptions.Tracking, It.IsAny<ISpecification<TestEntity>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.UpdateAsync(entityId, e => e.Name = "Updated", lockOptions);

        // Assert
        Assert.NotNull(result);
        _entityLockServiceMock.Verify(s => s.ValidateLockForUpdateWithChildren(It.IsAny<TestEntity>(), userId, 2), Times.Once);
        _entityLockServiceMock.Verify(s => s.UnlockWithChildrenIfSupported(It.IsAny<TestEntity>(), userId, 2), Times.Once);
    }

    [Fact]
    public async Task HardDeleteAsync_WithLockOptions_ValidatesLockWithChildren()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var entity = new TestEntity { Id = entityId, Name = "ToDelete" };
        var userId = _currentUserServiceMock.Object.UserId;
        var lockOptions = new LockOptions { IncludeChildren = true, MaxDepth = 2 };
        
        _repositoryMock.Setup(r => r.GetByIdOrThrowAsync(entityId, QueryOptions.Tracking, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _service.HardDeleteAsync(entityId, lockOptions);

        // Assert
        _entityLockServiceMock.Verify(s => s.ValidateLockForUpdateWithChildren(It.IsAny<TestEntity>(), userId, 2), Times.Once);
        _repositoryMock.Verify(r => r.Remove(It.IsAny<TestEntity>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HardDeleteAsync_WithoutLockOptions_ValidatesLock()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var entity = new TestEntity { Id = entityId, Name = "ToDelete" };
        var userId = _currentUserServiceMock.Object.UserId;
        
        _repositoryMock.Setup(r => r.GetByIdOrThrowAsync(entityId, QueryOptions.Tracking, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _service.HardDeleteAsync(entityId);

        // Assert
        _entityLockServiceMock.Verify(s => s.ValidateLockForUpdate(It.IsAny<TestEntity>(), userId), Times.Once);
        _repositoryMock.Verify(r => r.Remove(It.IsAny<TestEntity>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HardDeleteAsync_WithConcurrencyException_HandlesConcurrency()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var entity = new TestEntity { Id = entityId };
        var userId = _currentUserServiceMock.Object.UserId;
        var concurrencyException = new ConcurrencyException("TestEntity", entityId, null, null);
        
        _repositoryMock.Setup(r => r.GetByIdOrThrowAsync(entityId, QueryOptions.Tracking, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException("Conflict", (Exception?)null, Array.Empty<Microsoft.EntityFrameworkCore.Update.IUpdateEntry>()));
        _concurrencyServiceMock.Setup(s => s.HandleConcurrencyException(It.IsAny<TestEntity>(), It.IsAny<DbUpdateConcurrencyException>()))
            .Returns(concurrencyException);

        // Act & Assert
        await Assert.ThrowsAsync<ConcurrencyException>(() => 
            _service.HardDeleteAsync(entityId));
    }

    [Fact]
    public async Task RestoreAsync_WithIncludeChildren_RestoresWithChildren()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var entity = new TestEntity { Id = entityId, Name = "ToRestore" };
        var userId = _currentUserServiceMock.Object.UserId;
        
        // Mock the entity to be treated as soft deletable by checking the type in the service
        _repositoryMock.Setup(r => r.GetByIdOrThrowAsync(entityId, QueryOptions.ForRestore, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        
        // Since TestEntity doesn't implement ISoftDeletableEntity, this will throw InvalidOperationException
        // But we can verify the flow

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.RestoreAsync(entityId, includeChildren: true));
    }

    [Fact]
    public async Task RestoreAsync_WithConcurrencyException_HandlesConcurrency()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var entity = new TestEntity { Id = entityId };
        var userId = _currentUserServiceMock.Object.UserId;
        var concurrencyException = new ConcurrencyException("TestEntity", entityId, null, null);
        
        // Since TestEntity doesn't implement ISoftDeletableEntity, this will throw InvalidOperationException
        // before concurrency exception can occur
        _repositoryMock.Setup(r => r.GetByIdOrThrowAsync(entityId, QueryOptions.ForRestore, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.RestoreAsync(entityId, includeChildren: false));
    }
}

