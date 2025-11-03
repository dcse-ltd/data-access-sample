using System.Linq.Expressions;
using Infrastructure.Repository.Interfaces;
using Infrastructure.Repository.Specification;
using Infrastructure.Tests.Helpers;
using Moq;

namespace Infrastructure.Tests.Repository.Specification;

public class AndSpecificationTests
{
    [Fact]
    public void Constructor_WithNullLeft_ThrowsArgumentNullException()
    {
        // Arrange
        var right = new Mock<ISpecification<TestEntity>>().Object;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AndSpecification<TestEntity>(null!, right));
    }

    [Fact]
    public void Constructor_WithNullRight_ThrowsArgumentNullException()
    {
        // Arrange
        var left = new Mock<ISpecification<TestEntity>>().Object;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AndSpecification<TestEntity>(left, null!));
    }

    [Fact]
    public void Constructor_WithBothCriteria_CombinesWithAnd()
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
        var combined = new AndSpecification<TestEntity>(left.Object, right.Object);

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
        var combined = new AndSpecification<TestEntity>(left.Object, right.Object);

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
        var combined = new AndSpecification<TestEntity>(left.Object, right.Object);

        // Assert
        Assert.Equal(2, combined.Includes.Count);
    }

    [Fact]
    public void Constructor_UsesLeftOrdering()
    {
        // Arrange
        var left = new Mock<ISpecification<TestEntity>>();
        var right = new Mock<ISpecification<TestEntity>>();
        
        Expression<Func<TestEntity, object>> leftOrderBy = e => e.Name;
        Expression<Func<TestEntity, object>> rightOrderBy = e => e.Value;
        
        left.Setup(s => s.Criteria).Returns((Expression<Func<TestEntity, bool>>?)null);
        right.Setup(s => s.Criteria).Returns((Expression<Func<TestEntity, bool>>?)null);
        left.Setup(s => s.Includes).Returns(new List<Expression<Func<TestEntity, object>>>());
        right.Setup(s => s.Includes).Returns(new List<Expression<Func<TestEntity, object>>>());
        left.Setup(s => s.IncludeStrings).Returns(new List<string>());
        right.Setup(s => s.IncludeStrings).Returns(new List<string>());
        left.Setup(s => s.OrderBy).Returns(leftOrderBy);
        right.Setup(s => s.OrderBy).Returns(rightOrderBy);
        left.Setup(s => s.OrderByDescending).Returns((Expression<Func<TestEntity, object>>?)null);
        right.Setup(s => s.OrderByDescending).Returns((Expression<Func<TestEntity, object>>?)null);
        left.Setup(s => s.Skip).Returns((int?)null);
        right.Setup(s => s.Skip).Returns((int?)null);
        left.Setup(s => s.Take).Returns((int?)null);
        right.Setup(s => s.Take).Returns((int?)null);
        left.Setup(s => s.AsNoTracking).Returns(true);
        right.Setup(s => s.AsNoTracking).Returns(true);

        // Act
        var combined = new AndSpecification<TestEntity>(left.Object, right.Object);

        // Assert
        Assert.Equal(leftOrderBy, combined.OrderBy);
    }

    [Fact]
    public void Constructor_TrackingEnabledIfEitherWantsTracking()
    {
        // Arrange
        var left = new Mock<ISpecification<TestEntity>>();
        var right = new Mock<ISpecification<TestEntity>>();
        
        left.Setup(s => s.Criteria).Returns((Expression<Func<TestEntity, bool>>?)null);
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
        left.Setup(s => s.AsNoTracking).Returns(false); // Wants tracking (AsNoTracking = false)
        right.Setup(s => s.AsNoTracking).Returns(true);  // No tracking (AsNoTracking = true)

        // Act
        var combined = new AndSpecification<TestEntity>(left.Object, right.Object);

        // Assert
        // AsNoTracking = left.AsNoTracking && right.AsNoTracking
        // false && true = false, so AsNoTracking = false (tracking enabled)
        // Tracking is enabled when EITHER specification wants tracking
        Assert.False(combined.AsNoTracking); // Tracking is enabled because left wants tracking
    }
}

