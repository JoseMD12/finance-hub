using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FinanceHub.TransactionAggregator.Application.Interfaces;
using FinanceHub.TransactionAggregator.Application.Queries.GetCategories;
using FinanceHub.TransactionAggregator.Domain.Entities;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FinanceHub.Tests.TransactionAggregator.Application;

public class GetCategoriesQueryHandlerTests
{
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly GetCategoriesQueryHandler _handler;

    public GetCategoriesQueryHandlerTests()
    {
        _handler = new GetCategoriesQueryHandler(_categoryRepository);
    }

    [Fact]
    public async Task Handle_ShouldReturnHierarchicalCategories_WhenCategoriesExist()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var parent = Category.Create("Alimentação", "food", "utensils", "emerald", isSystemDefault: true, id: parentId);
        var sub1 = Category.Create("Supermercado", "food-supermarket", "shopping-cart", "emerald", isSystemDefault: true, parentCategoryId: parentId);
        var sub2 = Category.Create("Restaurante", "food-restaurant", "utensils", "emerald", isSystemDefault: true, parentCategoryId: parentId);

        _categoryRepository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Category> { parent, sub1, sub2 });

        // Act
        var result = (await _handler.Handle(new GetCategoriesQuery(), CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(1);
        var firstParent = result[0];
        firstParent.Id.Should().Be(parentId);
        firstParent.Name.Should().Be("Alimentação");
        firstParent.Subcategories.Should().NotBeNull();
        firstParent.Subcategories.Should().HaveCount(2);
        firstParent.Subcategories!.Select(s => s.Name).Should().Contain(new[] { "Supermercado", "Restaurante" });
    }
}
