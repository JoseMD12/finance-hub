using System;
using System.Linq;
using FinanceHub.TransactionAggregator.Infrastructure.Persistence;
using FluentAssertions;
using Xunit;

namespace FinanceHub.Tests.TransactionAggregator.Infrastructure;

public class CategorySeedDataTests
{
    [Fact]
    public void GetDefaultCategories_ShouldLoadAllCategoriesAndSubcategoriesFromJson()
    {
        // Act
        var categories = CategorySeedData.GetDefaultCategories();

        // Assert
        categories.Should().NotBeNullOrEmpty();
        
        var parents = categories.Where(c => c.ParentCategoryId == null).ToList();
        parents.Should().HaveCount(10);
        parents.Select(p => p.Name).Should().Contain(new[]
        {
            "Alimentação", "Transporte", "Moradia", "Saúde", "Lazer",
            "Compras", "Educação", "Finanças", "Receitas", "Outros"
        });

        var subcategories = categories.Where(c => c.ParentCategoryId != null).ToList();
        subcategories.Should().NotBeEmpty();

        // Alimentação subcategories check
        var foodParent = parents.First(p => p.Slug == "food");
        var foodSubs = subcategories.Where(s => s.ParentCategoryId == foodParent.Id).ToList();
        foodSubs.Select(s => s.Name).Should().Contain(new[] { "Supermercado", "Restaurante", "Delivery", "Padaria" });

        // Check that all categories have valid non-empty IDs
        categories.Should().OnlyContain(c => c.Id != Guid.Empty);
    }
}
