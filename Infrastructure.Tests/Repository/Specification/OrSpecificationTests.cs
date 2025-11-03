using System.Linq.Expressions;
using Infrastructure.Repository.Interfaces;
using Infrastructure.Repository.Specification;
using Infrastructure.Tests.Helpers;
using Moq;

namespace Infrastructure.Tests.Repository.Specification;

public class OrSpecificationTests
{
    [Fact]
    public void Constructor_WithNullLeft_ThrowsArgumentNullException()
    {
        // Arrange
        var right = new Mock<ISpecification<TestEntity>>().Object;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new OrSpecification<TestEntity>(null!, right));
    }

    [Fact]
    public void Constructor_WithNullRight_ThrowsArgumentNullException()
    {
        // Arrange
        var left = new Mock<ISpecification<TestEntity>>().Object;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new OrSpecification<TestEntity>(left, null!));
    }

    [Fact]
    public void Constructor_WithBothCriteria_CombinesWithOr()
    {
        // Arrange
        var left = new Mock<ISpecification<TestEntity>>();
        var right = new Mock<ISpecification<TestEntity>>();
        
        left.Setup(s => s.Criteria).Returns((Expression<Func<TestEntity, bool>>)(e => e.IsActive));
        right.Setup(s => s.Criteria).Returns((Expression<Func<TestEntity, bool>>)(e => e.Value > 10));
        left.Setup(s => s.Includes).Returns(new List<Expression<Func<TestEntity, object>>>());
        right.Setup(s => s.Includes).Returns(new List<Expression<Func<TestEntity, object>>>());
        left.Setup(s => s.IncludeStrings).Returns(new List<string>());
        right.Setup(s => s.IncludeStrings).Returns(new List<string>());
        left.Setup(s => s.OrderBy).Returns((Expression<Func<TestEntity, object>>?)null);
        right.Setup(s => s.OrderBy).Returns((Expression<Func<TestEntity, object>>?)null);
        left.Setup(s => s.OrderByDescending).Returns((Expression<Func<TestEntity, object>>?)null);
        right.Setup(s => s.OrderByDescending).Returns((Expression<Func<TestEntity, object>>?)null);
        left.Setup(s => s.Skip).Returns((int?)null);
        right.Setup(s => s.Skip).Returns((int?)null);
        left.Setup(s => s.Take).Returns((int?)null);
        right.Setup(s => s.Take).Returns((int?)null);
        left.Setup(s => s.AsNoTracking).Returns(true);
        right.Setup(s => s.AsNoTracking).Returns(true);

        // Act
        var combined = new OrSpecification<TestEntity>(left.Object, right.Object);

        // Assert
        Assert.NotNull(combined.Criteria);
        Assert.True(combined.AsNoTracking); // Both want no tracking, so result should be no tracking
    }

    [Fact]
    public void Constructor_WithLeftCriteriaOnly_UsesLeftCriteria()
    {
        // Arrange
        var left = new Mock<ISpecification<TestEntity>>();
        var right = new Mock<ISpecification<TestEntity>>();
        
        left.Setup(s => s.Criteria).Returns((Expression<Func<TestEntity, bool>>)(e => e.IsActive));
        right.Setup(s => s.Criteria).Returns((Expression<Func<TestEntity, bool>>?)null);
        left.Setup(s => s.Includes).Returns(new List<Expression<Func<TestEntity, object>>>());
        right.Setup(s => s.Includes).Returns(new List<Expression<Func<TestEntity, object>>>());
        left.Setup(s => s.IncludeStrings).Returns(new List<string>());
        right.Setup(s => s.IncludeStrings).Returns(new List<string>());
        left.Setup(s => s.OrderBy).Returns((Expression<Func<TestEntity, object>>?)null);
        right.Setup(s => s.OrderBy).Returns((Expression<Func<TestEntity, object>>?)null);
        left.Setup(s => s.OrderByDescending).Returns((Expression<Func<TestEntity, object>>?)null);
        right.Setup(s => s.OrderByDescending).Returns((Expression<Func<TestEntity, object>>?)null);
        left.Setup(s => s.Skip).Returns((int?)null);
        right.Setup(s => s.Skip).Returns((int?)null);
        left.Setup(s => s.Take).Returns((int?)null);
        right.Setup(s => s.Take).Returns((int?)null);
        left.Setup(s => s.AsNoTracking).Returns(true);
        right.Setup(s => s.AsNoTracking).Returns(true);

        // Act
        var combined = new OrSpecification<TestEntity>(left.Object, right.Object);

        // Assert
        Assert.NotNull(combined.Criteria);
    }

    [Fact]
    public void Constructor_MergesIncludes()
    {
        // Arrange
        var left = new Mock<ISpecification<TestEntity>>();
        var right = new Mock<ISpecification<TestEntity>>();
        
        var leftInclude = new List<Expression<Func<TestEntity, object>>> { e => e.Name };
        var rightInclude = new List<Expression<Func<TestEntity, object>>> { e => e.Value };
        
        left.Setup(s => s.Criteria).Returns((Expression<Func<TestEntity, bool>>?)null);
        right.Setup(s => s.Criteria).Returns((Expression<Func<TestEntity, bool>>?)null);
        left.Setup(s => s.Includes).Returns(leftInclude);
        right.Setup(s => s.Includes).Returns(rightInclude);
        left.Setup(s => s.IncludeStrings).Returns(new List<string>());
        right.Setup(s => s.IncludeStrings).Returns(new List<string>());
        left.Setup(s => s.OrderBy).Returns((Expression<Func<TestEntity, object>>?)null);
        right.Setup(s => s.OrderBy).Returns((Expression<Func<TestEntity, object>>?)null);
        left.Setup(s => s.OrderByDescending).Returns((Expression<Func<TestEntity, object>>?)null);
        right.Setup(s => s.OrderByDescending).Returns((Expression<Func<TestEntity, object>>?)null);
        left.Setup(s => s.Skip).Returns((int?)null);
        right.Setup(s => s.Skip).Returns((int?)null);
        left.Setup(s => s.Take).Returns((int?)null);
        right.Setup(s => s.Take).Returns((int?)null);
        left.Setup(s => s.AsNoTracking).Returns(true);
        right.Setup(s => s.AsNoTracking).Returns(true);

        // Act
        var combined = new OrSpecification<TestEntity>(left.Object, right.Object);

        // Assert
        Assert.Equal(2, combined.Includes.Count);
    }
}

