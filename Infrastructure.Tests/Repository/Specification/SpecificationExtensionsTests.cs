using Infrastructure.Repository.Interfaces;
using Infrastructure.Repository.Specification;
using Infrastructure.Tests.Helpers;
using Moq;
using System.Linq.Expressions;

namespace Infrastructure.Tests.Repository.Specification;

public class SpecificationExtensionsTests
{
    [Fact]
    public void And_WithNullLeft_ThrowsArgumentNullException()
    {
        // Arrange
        var right = new Mock<ISpecification<TestEntity>>().Object;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((ISpecification<TestEntity>)null!).And(right));
    }

    [Fact]
    public void And_WithNullRight_ThrowsArgumentNullException()
    {
        // Arrange
        var left = new Mock<ISpecification<TestEntity>>().Object;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => left.And(null!));
    }

    [Fact]
    public void And_WithValidSpecifications_ReturnsAndSpecification()
    {
        // Arrange
        var left = new Mock<ISpecification<TestEntity>>();
        left.Setup(s => s.Includes).Returns(new List<Expression<Func<TestEntity, object>>>());
        left.Setup(s => s.IncludeStrings).Returns(new List<string>());
        
        var right = new Mock<ISpecification<TestEntity>>();
        right.Setup(s => s.Includes).Returns(new List<Expression<Func<TestEntity, object>>>());
        right.Setup(s => s.IncludeStrings).Returns(new List<string>());

        // Act
        var result = left.Object.And(right.Object);

        // Assert
        Assert.IsType<AndSpecification<TestEntity>>(result);
    }

    [Fact]
    public void Or_WithNullLeft_ThrowsArgumentNullException()
    {
        // Arrange
        var right = new Mock<ISpecification<TestEntity>>().Object;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((ISpecification<TestEntity>)null!).Or(right));
    }

    [Fact]
    public void Or_WithNullRight_ThrowsArgumentNullException()
    {
        // Arrange
        var left = new Mock<ISpecification<TestEntity>>().Object;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => left.Or(null!));
    }

    [Fact]
    public void Or_WithValidSpecifications_ReturnsOrSpecification()
    {
        // Arrange
        var left = new Mock<ISpecification<TestEntity>>();
        left.Setup(s => s.Includes).Returns(new List<Expression<Func<TestEntity, object>>>());
        left.Setup(s => s.IncludeStrings).Returns(new List<string>());
        
        var right = new Mock<ISpecification<TestEntity>>();
        right.Setup(s => s.Includes).Returns(new List<Expression<Func<TestEntity, object>>>());
        right.Setup(s => s.IncludeStrings).Returns(new List<string>());

        // Act
        var result = left.Object.Or(right.Object);

        // Assert
        Assert.IsType<OrSpecification<TestEntity>>(result);
    }
}

