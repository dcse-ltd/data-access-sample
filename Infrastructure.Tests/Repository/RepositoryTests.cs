using Infrastructure.Exceptions;
using Infrastructure.Repository;
using Infrastructure.Repository.Interfaces;
using Infrastructure.Repository.Models;
using Infrastructure.Repository.Specification;
using Infrastructure.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests.Repository;

public class RepositoryTests
{
    private TestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new TestDbContext(options);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingEntity_ReturnsEntity()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new Repository<TestEntity>(context);
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Test" };
        context.TestEntities.Add(entity);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByIdAsync(entity.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(entity.Id, result.Id);
        Assert.Equal(entity.Name, result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingEntity_ReturnsNull()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new Repository<TestEntity>(context);
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await repository.GetByIdAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WithCancellationToken_ThrowsWhenCancelled()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new Repository<TestEntity>(context);
        var entity = new TestEntity { Id = Guid.NewGuid() };
        context.TestEntities.Add(entity);
        await context.SaveChangesAsync();
        
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => 
            repository.GetByIdAsync(entity.Id, cts.Token));
    }

    [Fact]
    public async Task GetByIdOrThrowAsync_WithExistingEntity_ReturnsEntity()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new Repository<TestEntity>(context);
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Test" };
        context.TestEntities.Add(entity);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByIdOrThrowAsync(entity.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(entity.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdOrThrowAsync_WithNonExistingEntity_ThrowsEntityNotFoundException()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new Repository<TestEntity>(context);
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() => 
            repository.GetByIdOrThrowAsync(nonExistentId));
        Assert.Equal(nonExistentId, exception.EntityId);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new Repository<TestEntity>(context);
        var entities = new[]
        {
            new TestEntity { Id = Guid.NewGuid(), Name = "Entity1" },
            new TestEntity { Id = Guid.NewGuid(), Name = "Entity2" },
            new TestEntity { Id = Guid.NewGuid(), Name = "Entity3" }
        };
        context.TestEntities.AddRange(entities);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetAllAsync();

        // Assert
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task AddAsync_AddsEntityToContext()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new Repository<TestEntity>(context);
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "New Entity" };

        // Act
        await repository.AddAsync(entity, CancellationToken.None);
        await context.SaveChangesAsync();

        // Assert
        Assert.True(context.TestEntities.Any(e => e.Id == entity.Id));
    }

    [Fact]
    public async Task FindAsync_WithSpecification_ReturnsMatchingEntities()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new Repository<TestEntity>(context);
        var entities = new[]
        {
            new TestEntity { Id = Guid.NewGuid(), Name = "Active", IsActive = true },
            new TestEntity { Id = Guid.NewGuid(), Name = "Inactive", IsActive = false },
            new TestEntity { Id = Guid.NewGuid(), Name = "Active2", IsActive = true }
        };
        context.TestEntities.AddRange(entities);
        await context.SaveChangesAsync();

        var spec = new TestSpecification();
        spec.AddCriteria(e => e.IsActive);

        // Act
        var result = await repository.FindAsync(spec, QueryOptions.Default);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, e => Assert.True(e.IsActive));
    }

    [Fact]
    public async Task FindPagedAsync_WithPagination_ReturnsPagedResult()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new Repository<TestEntity>(context);
        var entities = Enumerable.Range(1, 10)
            .Select(i => new TestEntity { Id = Guid.NewGuid(), Name = $"Entity{i}", Value = i })
            .ToArray();
        context.TestEntities.AddRange(entities);
        await context.SaveChangesAsync();

        var spec = new TestSpecification();
        spec.AddCriteria(e => e.Value > 0);
        spec.ApplyOrderBy(e => e.Value);
        spec.ApplyPaging(2, 3); // Skip 2, take 3

        // Act
        var result = await repository.FindPagedAsync(spec, QueryOptions.Default);

        // Assert
        Assert.Equal(3, result.Items.Count());
        Assert.Equal(10, result.TotalCount);
        // Page is calculated as (skip / take) + 1 = (2 / 3) + 1 = 0 + 1 = 1
        // But if skip=2 and take=3, we're on page 2 (0-indexed would be page 1, but we use 1-based)
        // Actually: skip=2 means we've skipped 2 items, so we're on page 2 (if pageSize=3, page1=items 0-2, page2=items 3-5)
        // So page = (skip / take) + 1 = (2 / 3) + 1 = 0 + 1 = 1? No wait...
        // If take=3: page1=skip0 take3, page2=skip3 take3, page3=skip6 take3
        // But we have skip=2, take=3, which doesn't align with standard pagination
        // The repository calculates: page = (skip / take) + 1 = (2 / 3) + 1 = 1
        Assert.Equal(1, result.Page); // Page is calculated as (skip / take) + 1
        Assert.Equal(3, result.PageSize);
        Assert.Equal(3, result.Items.First().Value); // Starts at 3 (after skipping 2, but skip=2 means skip items 0-1, so starts at 3)
    }

    [Fact]
    public async Task FindOneAsync_WithSpecification_ReturnsFirstMatchingEntity()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new Repository<TestEntity>(context);
        var entities = new[]
        {
            new TestEntity { Id = Guid.NewGuid(), Name = "First", Value = 1 },
            new TestEntity { Id = Guid.NewGuid(), Name = "Second", Value = 2 },
            new TestEntity { Id = Guid.NewGuid(), Name = "Third", Value = 3 }
        };
        context.TestEntities.AddRange(entities);
        await context.SaveChangesAsync();

        var spec = new TestSpecification();
        spec.AddCriteria(e => e.Value > 1);
        spec.ApplyOrderBy(e => e.Value);

        // Act
        var result = await repository.FindOneAsync(spec, QueryOptions.Default);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Value);
    }

    [Fact]
    public async Task CountAsync_WithSpecification_ReturnsCorrectCount()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new Repository<TestEntity>(context);
        var entities = new[]
        {
            new TestEntity { Id = Guid.NewGuid(), IsActive = true },
            new TestEntity { Id = Guid.NewGuid(), IsActive = true },
            new TestEntity { Id = Guid.NewGuid(), IsActive = false }
        };
        context.TestEntities.AddRange(entities);
        await context.SaveChangesAsync();

        var spec = new TestSpecification();
        spec.AddCriteria(e => e.IsActive);

        // Act
        var result = await repository.CountAsync(spec, QueryOptions.Default);

        // Assert
        Assert.Equal(2, result);
    }

    [Fact]
    public async Task ExistsAsync_WithExistingEntity_ReturnsTrue()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new Repository<TestEntity>(context);
        var entity = new TestEntity { Id = Guid.NewGuid() };
        context.TestEntities.Add(entity);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.ExistsAsync(entity.Id, QueryOptions.Default);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistingEntity_ReturnsFalse()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new Repository<TestEntity>(context);
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await repository.ExistsAsync(nonExistentId, QueryOptions.Default);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task AnyAsync_WithMatchingSpecification_ReturnsTrue()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new Repository<TestEntity>(context);
        var entities = new[]
        {
            new TestEntity { Id = Guid.NewGuid(), IsActive = true },
            new TestEntity { Id = Guid.NewGuid(), IsActive = false }
        };
        context.TestEntities.AddRange(entities);
        await context.SaveChangesAsync();

        var spec = new TestSpecification();
        spec.AddCriteria(e => e.IsActive);

        // Act
        var result = await repository.AnyAsync(spec, QueryOptions.Default);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task AnyAsync_WithNonMatchingSpecification_ReturnsFalse()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new Repository<TestEntity>(context);
        var entities = new[]
        {
            new TestEntity { Id = Guid.NewGuid(), IsActive = false }
        };
        context.TestEntities.AddRange(entities);
        await context.SaveChangesAsync();

        var spec = new TestSpecification();
        spec.AddCriteria(e => e.IsActive);

        // Act
        var result = await repository.AnyAsync(spec, QueryOptions.Default);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Update_MarksEntityAsModified()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new Repository<TestEntity>(context);
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Original" };
        context.TestEntities.Add(entity);
        context.SaveChanges();

        entity.Name = "Modified";

        // Act
        repository.Update(entity);

        // Assert
        var entry = context.Entry(entity);
        Assert.Equal(EntityState.Modified, entry.State);
    }

    [Fact]
    public void Remove_MarksEntityAsDeleted()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new Repository<TestEntity>(context);
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "ToRemove" };
        context.TestEntities.Add(entity);
        context.SaveChanges();

        // Act
        repository.Remove(entity);

        // Assert
        var entry = context.Entry(entity);
        Assert.Equal(EntityState.Deleted, entry.State);
    }

    private class TestSpecification : BaseSpecification<TestEntity>
    {
        public void AddCriteria(System.Linq.Expressions.Expression<Func<TestEntity, bool>> criteria)
        {
            base.AddCriteria(criteria);
        }

        public void ApplyOrderBy(System.Linq.Expressions.Expression<Func<TestEntity, object>> orderBy)
        {
            base.ApplyOrderBy(orderBy);
        }

        public void ApplyPaging(int skip, int take)
        {
            base.ApplyPaging(skip, take);
        }
    }
}

