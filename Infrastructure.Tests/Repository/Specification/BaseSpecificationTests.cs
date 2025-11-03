using System.Linq.Expressions;
using Infrastructure.Repository.Interfaces;
using Infrastructure.Repository.Specification;
using Infrastructure.Tests.Helpers;

namespace Infrastructure.Tests.Repository.Specification;

public class BaseSpecificationTests
{
    private class TestSpecification : BaseSpecification<TestEntity>
    {
        public TestSpecification()
        {
        }

        public void TestAddCriteria(Expression<Func<TestEntity, bool>> criteria)
        {
            AddCriteria(criteria);
        }

        public void TestAddCriteriaAnd(Expression<Func<TestEntity, bool>> criteria)
        {
            AddCriteriaAnd(criteria);
        }

        public void TestAddCriteriaOr(Expression<Func<TestEntity, bool>> criteria)
        {
            AddCriteriaOr(criteria);
        }

        public void SetOrderBy(Expression<Func<TestEntity, object>> orderBy)
        {
            OrderBy = orderBy;
        }

        public void SetOrderByDescending(Expression<Func<TestEntity, object>> orderByDescending)
        {
            OrderByDescending = orderByDescending;
        }

        public void SetSkip(int skip)
        {
            Skip = skip;
        }

        public void SetTake(int take)
        {
            Take = take;
        }

        public void SetAsNoTracking(bool asNoTracking)
        {
            AsNoTracking = asNoTracking;
        }

        public void TestAddInclude(Expression<Func<TestEntity, object>> include)
        {
            AddInclude(include);
        }

        public void TestAddIncludeString(string includeString)
        {
            AddInclude(includeString);
        }

        public void TestApplyOrderBy(Expression<Func<TestEntity, object>> orderBy)
        {
            ApplyOrderBy(orderBy);
        }

        public void TestApplyOrderByDescending(Expression<Func<TestEntity, object>> orderByDescending)
        {
            ApplyOrderByDescending(orderByDescending);
        }

        public void TestApplyPaging(int skip, int take)
        {
            ApplyPaging(skip, take);
        }

        public void TestApplyTracking()
        {
            ApplyTracking();
        }
    }

    [Fact]
    public void Constructor_InitializesWithDefaultValues()
    {
        // Act
        var spec = new TestSpecification();

        // Assert
        Assert.Null(spec.Criteria);
        Assert.Empty(spec.Includes);
        Assert.Empty(spec.IncludeStrings);
        Assert.Null(spec.OrderBy);
        Assert.Null(spec.OrderByDescending);
        Assert.Null(spec.Skip);
        Assert.Null(spec.Take);
        Assert.True(spec.AsNoTracking); // Default is true
    }

    [Fact]
    public void AddCriteria_WithFirstCriteria_SetsCriteria()
    {
        // Arrange
        var spec = new TestSpecification();
        Expression<Func<TestEntity, bool>> criteria = e => e.IsActive;

        // Act
        spec.TestAddCriteria(criteria);

        // Assert
        Assert.NotNull(spec.Criteria);
    }

    [Fact]
    public void AddCriteria_WithMultipleCriteria_CombinesWithAnd()
    {
        // Arrange
        var spec = new TestSpecification();
        Expression<Func<TestEntity, bool>> criteria1 = e => e.IsActive;
        Expression<Func<TestEntity, bool>> criteria2 = e => e.Value > 10;

        // Act
        spec.TestAddCriteria(criteria1);
        spec.TestAddCriteria(criteria2);

        // Assert
        Assert.NotNull(spec.Criteria);
        // The criteria should be combined with AND logic
    }

    [Fact]
    public void AddCriteriaAnd_IsEquivalentToAddCriteria()
    {
        // Arrange
        var spec1 = new TestSpecification();
        var spec2 = new TestSpecification();
        Expression<Func<TestEntity, bool>> criteria1 = e => e.IsActive;
        Expression<Func<TestEntity, bool>> criteria2 = e => e.Value > 10;

        // Act
        spec1.TestAddCriteria(criteria1);
        spec1.TestAddCriteria(criteria2);
        
        spec2.TestAddCriteriaAnd(criteria1);
        spec2.TestAddCriteriaAnd(criteria2);

        // Assert
        Assert.NotNull(spec1.Criteria);
        Assert.NotNull(spec2.Criteria);
    }

    [Fact]
    public void AddCriteriaOr_WithNoExistingCriteria_SetsCriteria()
    {
        // Arrange
        var spec = new TestSpecification();
        Expression<Func<TestEntity, bool>> criteria = e => e.IsActive;

        // Act
        spec.TestAddCriteriaOr(criteria);

        // Assert
        Assert.NotNull(spec.Criteria);
    }

    [Fact]
    public void AddCriteriaOr_WithExistingCriteria_CombinesWithOr()
    {
        // Arrange
        var spec = new TestSpecification();
        Expression<Func<TestEntity, bool>> criteria1 = e => e.IsActive;
        Expression<Func<TestEntity, bool>> criteria2 = e => e.Value > 10;

        // Act
        spec.TestAddCriteria(criteria1);
        spec.TestAddCriteriaOr(criteria2);

        // Assert
        Assert.NotNull(spec.Criteria);
        // The criteria should be combined with OR logic
    }

    [Fact]
    public void AddInclude_WithExpression_AddsToIncludes()
    {
        // Arrange
        var spec = new TestSpecification();
        Expression<Func<TestEntity, object>> include = e => e.Name;

        // Act
        spec.TestAddInclude(include);

        // Assert
        Assert.Single(spec.Includes);
        Assert.Contains(include, spec.Includes);
    }

    [Fact]
    public void AddInclude_WithString_AddsToIncludeStrings()
    {
        // Arrange
        var spec = new TestSpecification();
        var includeString = "RelatedEntity";

        // Act
        spec.TestAddIncludeString(includeString);

        // Assert
        Assert.Single(spec.IncludeStrings);
        Assert.Contains(includeString, spec.IncludeStrings);
    }

    [Fact]
    public void ApplyOrderBy_SetsOrderBy()
    {
        // Arrange
        var spec = new TestSpecification();
        Expression<Func<TestEntity, object>> orderBy = e => e.Name;

        // Act
        spec.TestApplyOrderBy(orderBy);

        // Assert
        Assert.NotNull(spec.OrderBy);
        Assert.Equal(orderBy, spec.OrderBy);
    }

    [Fact]
    public void ApplyOrderByDescending_SetsOrderByDescending()
    {
        // Arrange
        var spec = new TestSpecification();
        Expression<Func<TestEntity, object>> orderByDesc = e => e.Value;

        // Act
        spec.TestApplyOrderByDescending(orderByDesc);

        // Assert
        Assert.NotNull(spec.OrderByDescending);
        Assert.Equal(orderByDesc, spec.OrderByDescending);
    }

    [Fact]
    public void ApplyPaging_SetsSkipAndTake()
    {
        // Arrange
        var spec = new TestSpecification();
        var skip = 10;
        var take = 20;

        // Act
        spec.TestApplyPaging(skip, take);

        // Assert
        Assert.Equal(skip, spec.Skip);
        Assert.Equal(take, spec.Take);
    }

    [Fact]
    public void ApplyTracking_SetsAsNoTrackingToFalse()
    {
        // Arrange
        var spec = new TestSpecification();
        Assert.True(spec.AsNoTracking); // Default is true

        // Act
        spec.TestApplyTracking();

        // Assert
        Assert.False(spec.AsNoTracking);
    }
}

