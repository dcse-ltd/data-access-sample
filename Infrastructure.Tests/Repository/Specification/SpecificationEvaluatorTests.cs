using Infrastructure.Repository.Interfaces;
using Infrastructure.Repository.Specification;
using Infrastructure.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Linq.Expressions;

namespace Infrastructure.Tests.Repository.Specification;

public class SpecificationEvaluatorTests
{
    private TestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new TestDbContext(options);
    }

    [Fact]
    public void GetQuery_WithCriteria_FiltersResults()
    {
        // Arrange
        using var context = CreateContext();
        var entities = new[]
        {
            new TestEntity { Id = Guid.NewGuid(), Name = "Entity1", IsActive = true, Value = 10 },
            new TestEntity { Id = Guid.NewGuid(), Name = "Entity2", IsActive = false, Value = 20 },
            new TestEntity { Id = Guid.NewGuid(), Name = "Entity3", IsActive = true, Value = 30 }
        };
        context.TestEntities.AddRange(entities);
        context.SaveChanges();

        var spec = new Mock<ISpecification<TestEntity>>();
        spec.Setup(s => s.Criteria).Returns((Expression<Func<TestEntity, bool>>)(e => e.IsActive));
        spec.Setup(s => s.Includes).Returns(new List<Expression<Func<TestEntity, object>>>());
        spec.Setup(s => s.IncludeStrings).Returns(new List<string>());
        spec.Setup(s => s.OrderBy).Returns((Expression<Func<TestEntity, object>>?)null);
        spec.Setup(s => s.OrderByDescending).Returns((Expression<Func<TestEntity, object>>?)null);
        spec.Setup(s => s.Skip).Returns((int?)null);
        spec.Setup(s => s.Take).Returns((int?)null);

        var query = context.TestEntities.AsQueryable();

        // Act
        var result = SpecificationEvaluator.GetQuery(query, spec.Object);

        // Assert
        var results = result.ToList();
        Assert.Equal(2, results.Count);
        Assert.All(results, e => Assert.True(e.IsActive));
    }

    [Fact]
    public void GetQuery_WithOrderBy_OrdersResults()
    {
        // Arrange
        using var context = CreateContext();
        var entities = new[]
        {
            new TestEntity { Id = Guid.NewGuid(), Name = "C", Value = 3 },
            new TestEntity { Id = Guid.NewGuid(), Name = "A", Value = 1 },
            new TestEntity { Id = Guid.NewGuid(), Name = "B", Value = 2 }
        };
        context.TestEntities.AddRange(entities);
        context.SaveChanges();

        var spec = new Mock<ISpecification<TestEntity>>();
        spec.Setup(s => s.Criteria).Returns((Expression<Func<TestEntity, bool>>?)null);
        spec.Setup(s => s.Includes).Returns(new List<Expression<Func<TestEntity, object>>>());
        spec.Setup(s => s.IncludeStrings).Returns(new List<string>());
        spec.Setup(s => s.OrderBy).Returns((Expression<Func<TestEntity, object>>)(e => e.Name));
        spec.Setup(s => s.OrderByDescending).Returns((Expression<Func<TestEntity, object>>?)null);
        spec.Setup(s => s.Skip).Returns((int?)null);
        spec.Setup(s => s.Take).Returns((int?)null);

        var query = context.TestEntities.AsQueryable();

        // Act
        var result = SpecificationEvaluator.GetQuery(query, spec.Object);

        // Assert
        var results = result.ToList();
        Assert.Equal("A", results[0].Name);
        Assert.Equal("B", results[1].Name);
        Assert.Equal("C", results[2].Name);
    }

    [Fact]
    public void GetQuery_WithOrderByDescending_OrdersResultsDescending()
    {
        // Arrange
        using var context = CreateContext();
        var entities = new[]
        {
            new TestEntity { Id = Guid.NewGuid(), Name = "A", Value = 1 },
            new TestEntity { Id = Guid.NewGuid(), Name = "C", Value = 3 },
            new TestEntity { Id = Guid.NewGuid(), Name = "B", Value = 2 }
        };
        context.TestEntities.AddRange(entities);
        context.SaveChanges();

        var spec = new Mock<ISpecification<TestEntity>>();
        spec.Setup(s => s.Criteria).Returns((Expression<Func<TestEntity, bool>>?)null);
        spec.Setup(s => s.Includes).Returns(new List<Expression<Func<TestEntity, object>>>());
        spec.Setup(s => s.IncludeStrings).Returns(new List<string>());
        spec.Setup(s => s.OrderBy).Returns((Expression<Func<TestEntity, object>>?)null);
        spec.Setup(s => s.OrderByDescending).Returns((Expression<Func<TestEntity, object>>)(e => e.Value));
        spec.Setup(s => s.Skip).Returns((int?)null);
        spec.Setup(s => s.Take).Returns((int?)null);

        var query = context.TestEntities.AsQueryable();

        // Act
        var result = SpecificationEvaluator.GetQuery(query, spec.Object);

        // Assert
        var results = result.ToList();
        Assert.Equal(3, results[0].Value);
        Assert.Equal(2, results[1].Value);
        Assert.Equal(1, results[2].Value);
    }

    [Fact]
    public void GetQuery_WithPaging_AppliesSkipAndTake()
    {
        // Arrange
        using var context = CreateContext();
        var entities = Enumerable.Range(1, 10)
            .Select(i => new TestEntity { Id = Guid.NewGuid(), Name = $"Entity{i}", Value = i })
            .ToArray();
        context.TestEntities.AddRange(entities);
        context.SaveChanges();

        var spec = new Mock<ISpecification<TestEntity>>();
        spec.Setup(s => s.Criteria).Returns((Expression<Func<TestEntity, bool>>?)null);
        spec.Setup(s => s.Includes).Returns(new List<Expression<Func<TestEntity, object>>>());
        spec.Setup(s => s.IncludeStrings).Returns(new List<string>());
        spec.Setup(s => s.OrderBy).Returns((Expression<Func<TestEntity, object>>)(e => e.Value));
        spec.Setup(s => s.OrderByDescending).Returns((Expression<Func<TestEntity, object>>?)null);
        spec.Setup(s => s.Skip).Returns(3);
        spec.Setup(s => s.Take).Returns(4);

        var query = context.TestEntities.AsQueryable();

        // Act
        var result = SpecificationEvaluator.GetQuery(query, spec.Object);

        // Assert
        var results = result.ToList();
        Assert.Equal(4, results.Count);
        Assert.Equal(4, results[0].Value); // Skipped 3, so starts at 4
        Assert.Equal(7, results[3].Value);
    }

    [Fact]
    public void GetQuery_WithOrderByAndOrderByDescending_UsesOrderBy()
    {
        // Arrange
        using var context = CreateContext();
        var entities = new[]
        {
            new TestEntity { Id = Guid.NewGuid(), Name = "C", Value = 3 },
            new TestEntity { Id = Guid.NewGuid(), Name = "A", Value = 1 },
            new TestEntity { Id = Guid.NewGuid(), Name = "B", Value = 2 }
        };
        context.TestEntities.AddRange(entities);
        context.SaveChanges();

        var spec = new Mock<ISpecification<TestEntity>>();
        spec.Setup(s => s.Criteria).Returns((Expression<Func<TestEntity, bool>>?)null);
        spec.Setup(s => s.Includes).Returns(new List<Expression<Func<TestEntity, object>>>());
        spec.Setup(s => s.IncludeStrings).Returns(new List<string>());
        spec.Setup(s => s.OrderBy).Returns((Expression<Func<TestEntity, object>>)(e => e.Name));
        spec.Setup(s => s.OrderByDescending).Returns((Expression<Func<TestEntity, object>>)(e => e.Value));
        spec.Setup(s => s.Skip).Returns((int?)null);
        spec.Setup(s => s.Take).Returns((int?)null);

        var query = context.TestEntities.AsQueryable();

        // Act
        var result = SpecificationEvaluator.GetQuery(query, spec.Object);

        // Assert
        var results = result.ToList();
        // OrderBy takes precedence
        Assert.Equal("A", results[0].Name);
        Assert.Equal("B", results[1].Name);
        Assert.Equal("C", results[2].Name);
    }

    [Fact]
    public void GetQuery_WithoutSpecification_ReturnsOriginalQuery()
    {
        // Arrange
        using var context = CreateContext();
        var entities = new[]
        {
            new TestEntity { Id = Guid.NewGuid(), Name = "Entity1" },
            new TestEntity { Id = Guid.NewGuid(), Name = "Entity2" }
        };
        context.TestEntities.AddRange(entities);
        context.SaveChanges();

        var spec = new Mock<ISpecification<TestEntity>>();
        spec.Setup(s => s.Criteria).Returns((Expression<Func<TestEntity, bool>>?)null);
        spec.Setup(s => s.Includes).Returns(new List<Expression<Func<TestEntity, object>>>());
        spec.Setup(s => s.IncludeStrings).Returns(new List<string>());
        spec.Setup(s => s.OrderBy).Returns((Expression<Func<TestEntity, object>>?)null);
        spec.Setup(s => s.OrderByDescending).Returns((Expression<Func<TestEntity, object>>?)null);
        spec.Setup(s => s.Skip).Returns((int?)null);
        spec.Setup(s => s.Take).Returns((int?)null);

        var query = context.TestEntities.AsQueryable();

        // Act
        var result = SpecificationEvaluator.GetQuery(query, spec.Object);

        // Assert
        var results = result.ToList();
        Assert.Equal(2, results.Count);
    }
}

