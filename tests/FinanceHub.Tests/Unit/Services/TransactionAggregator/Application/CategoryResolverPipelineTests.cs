using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FinanceHub.TransactionAggregator.Application.Interfaces;
using FinanceHub.TransactionAggregator.Application.Services.Categorization;
using FinanceHub.TransactionAggregator.Domain.Entities;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FinanceHub.Tests.TransactionAggregator.Application;

public class CategoryResolverPipelineTests
{
    [Fact]
    public async Task Pipeline_ShouldReturnUserRuleCategory_WhenUserRuleMatches()
    {
        // Arrange
        var userRepo = Substitute.For<IUserCategoryRuleRepository>();
        var userCategoryId = Guid.NewGuid();
        userRepo.FindByPatternAsync("user-1", "SMARTFIT", Arg.Any<CancellationToken>())
            .Returns(UserCategoryRule.Create("user-1", "SMARTFIT", userCategoryId));

        var merchantProvider = Substitute.For<IMerchantDatasetProvider>();
        var resolvers = new List<ICategoryResolver>
        {
            new UserCustomRuleCategoryResolver(userRepo),
            new GlobalPatternCategoryResolver(merchantProvider),
            new DefaultFallbackCategoryResolver()
        };

        var pipeline = new CategoryResolverPipeline(resolvers);

        // Act
        var result = await pipeline.ResolveCategoryAsync("user-1", "PAG*SmartFit 12/08 SAO PAULO", CancellationToken.None);

        // Assert
        result.CategoryId.Should().Be(userCategoryId);
        result.Source.Should().Be(CategorizationSource.UserRule);
    }

    [Fact]
    public async Task Pipeline_ShouldReturnGlobalCategory_WhenNoUserRuleMatches_AndGlobalPatternMatches()
    {
        // Arrange
        var userRepo = Substitute.For<IUserCategoryRuleRepository>();
        userRepo.FindByPatternAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((UserCategoryRule?)null);

        var globalCategoryId = Guid.NewGuid();
        var merchantProvider = Substitute.For<IMerchantDatasetProvider>();
        merchantProvider.Match(Arg.Any<string>())
            .Returns(new MerchantDefinition("merchant-uber", "Uber", globalCategoryId, new[] { "UBER" }, "Uber"));

        var resolvers = new List<ICategoryResolver>
        {
            new UserCustomRuleCategoryResolver(userRepo),
            new GlobalPatternCategoryResolver(merchantProvider),
            new DefaultFallbackCategoryResolver()
        };

        var pipeline = new CategoryResolverPipeline(resolvers);

        // Act
        var result = await pipeline.ResolveCategoryAsync("user-1", "UBER *TRIP BR", CancellationToken.None);

        // Assert
        result.Source.Should().Be(CategorizationSource.GlobalRule);
        result.CategoryId.Should().Be(globalCategoryId);
    }

    [Fact]
    public async Task Pipeline_ShouldReturnFallbackCategory_WhenNoUserOrGlobalRuleMatches()
    {
        // Arrange
        var userRepo = Substitute.For<IUserCategoryRuleRepository>();
        userRepo.FindByPatternAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((UserCategoryRule?)null);

        var merchantProvider = Substitute.For<IMerchantDatasetProvider>();
        merchantProvider.Match(Arg.Any<string>()).Returns((MerchantDefinition?)null);

        var resolvers = new List<ICategoryResolver>
        {
            new UserCustomRuleCategoryResolver(userRepo),
            new GlobalPatternCategoryResolver(merchantProvider),
            new DefaultFallbackCategoryResolver()
        };

        var pipeline = new CategoryResolverPipeline(resolvers);

        // Act
        var result = await pipeline.ResolveCategoryAsync("user-1", "ESTABELECIMENTO DESCONHECIDO 999", CancellationToken.None);

        // Assert
        result.Source.Should().Be(CategorizationSource.Fallback);
    }
}
