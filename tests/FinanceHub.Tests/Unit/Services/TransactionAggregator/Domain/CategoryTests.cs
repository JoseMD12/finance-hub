using System;
using FinanceHub.TransactionAggregator.Domain.Entities;
using FinanceHub.TransactionAggregator.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace FinanceHub.Tests.TransactionAggregator.Domain;

public class CategoryTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldCreateCategorySuccessfully()
    {
        // Arrange & Act
        var category = Category.Create(
            name: "Alimentação",
            slug: "food",
            iconKey: "utensils",
            colorToken: "emerald",
            isSystemDefault: true);

        // Assert
        category.Id.Should().NotBeEmpty();
        category.Name.Should().Be("Alimentação");
        category.Slug.Should().Be("food");
        category.IconKey.Should().Be("utensils");
        category.ColorToken.Should().Be("emerald");
        category.ParentCategoryId.Should().BeNull();
        category.IsSystemDefault.Should().BeTrue();
        category.IsActive.Should().BeTrue();
        category.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_WithParentCategory_ShouldSetParentCategoryId()
    {
        // Arrange
        var parentId = Guid.NewGuid();

        // Act
        var subcategory = Category.Create(
            name: "Restaurante",
            slug: "restaurant",
            iconKey: "utensils",
            colorToken: "emerald",
            isSystemDefault: true,
            parentCategoryId: parentId);

        // Assert
        subcategory.ParentCategoryId.Should().Be(parentId);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithInvalidName_ShouldThrowDomainException(string? invalidName)
    {
        // Act
        var act = () => Category.Create(
            name: invalidName!,
            slug: "food",
            iconKey: "utensils",
            colorToken: "emerald");

        // Assert
        act.Should().Throw<InvalidCategoryNameDomainException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithInvalidSlug_ShouldThrowDomainException(string? invalidSlug)
    {
        // Act
        var act = () => Category.Create(
            name: "Alimentação",
            slug: invalidSlug!,
            iconKey: "utensils",
            colorToken: "emerald");

        // Assert
        act.Should().Throw<InvalidCategorySlugDomainException>();
    }

    [Fact]
    public void Deactivate_WhenActive_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var category = Category.Create("Transporte", "transport", "car", "sky", true);

        // Act
        category.Deactivate();

        // Assert
        category.IsActive.Should().BeFalse();
    }

    [Fact]
    public void UpdateDetails_WithValidData_ShouldUpdateProperties()
    {
        // Arrange
        var category = Category.Create("Alimentação", "food", "utensils", "emerald", false);

        // Act
        category.UpdateDetails("Alimentação & Mercado", "cart", "teal");

        // Assert
        category.Name.Should().Be("Alimentação & Mercado");
        category.IconKey.Should().Be("cart");
        category.ColorToken.Should().Be("teal");
    }
}
