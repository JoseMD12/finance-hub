using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FinanceHub.TransactionAggregator.Application.Interfaces;
using FinanceHub.TransactionAggregator.Domain.Entities;
using FinanceHub.TransactionAggregator.Domain.ValueObjects;

namespace FinanceHub.TransactionAggregator.Application.Services.Categorization;

public class UserCustomRuleCategoryResolver : ICategoryResolver
{
    private readonly IUserCategoryRuleRepository _userRuleRepository;

    public int Priority => 1;

    public UserCustomRuleCategoryResolver(IUserCategoryRuleRepository userRuleRepository)
    {
        _userRuleRepository = userRuleRepository;
    }

    public async Task<CategorizationResult?> ResolveAsync(string userId, SanitizedDescription description, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(description.CleanText))
            return null;

        var firstWord = description.CleanText.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(firstWord))
            return null;

        var rule = await _userRuleRepository.FindByPatternAsync(userId, firstWord, cancellationToken);
        if (rule == null)
            return null;

        return new CategorizationResult(rule.CategoryId, CategorizationSource.UserRule);
    }
}

public class GlobalPatternCategoryResolver : ICategoryResolver
{
    private static readonly Guid TransportCategoryId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid FoodCategoryId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly Guid ShoppingCategoryId = Guid.Parse("10000000-0000-0000-0000-000000000003");

    public int Priority => 2;

    public Task<CategorizationResult?> ResolveAsync(string userId, SanitizedDescription description, CancellationToken cancellationToken)
    {
        var text = description.CleanText.ToUpperInvariant();

        if (text.Contains("UBER") || text.Contains("99APP") || text.Contains("POSTO"))
        {
            return Task.FromResult<CategorizationResult?>(new CategorizationResult(TransportCategoryId, CategorizationSource.GlobalRule));
        }

        if (text.Contains("IFOOD") || text.Contains("RESTAURANTE") || text.Contains("PADARIA") || text.Contains("MCDONALDS"))
        {
            return Task.FromResult<CategorizationResult?>(new CategorizationResult(FoodCategoryId, CategorizationSource.GlobalRule));
        }

        if (text.Contains("AMAZON") || text.Contains("MERCADOLIVRE") || text.Contains("MAGALU"))
        {
            return Task.FromResult<CategorizationResult?>(new CategorizationResult(ShoppingCategoryId, CategorizationSource.GlobalRule));
        }

        return Task.FromResult<CategorizationResult?>(null);
    }
}

public class DefaultFallbackCategoryResolver : ICategoryResolver
{
    public static readonly Guid OthersCategoryId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    public int Priority => 3;

    public Task<CategorizationResult?> ResolveAsync(string userId, SanitizedDescription description, CancellationToken cancellationToken)
    {
        return Task.FromResult<CategorizationResult?>(new CategorizationResult(OthersCategoryId, CategorizationSource.Fallback));
    }
}

public class CategoryResolverPipeline : ICategoryResolverPipeline
{
    private readonly IEnumerable<ICategoryResolver> _resolvers;

    public CategoryResolverPipeline(IEnumerable<ICategoryResolver> resolvers)
    {
        _resolvers = resolvers.OrderBy(r => r.Priority);
    }

    public async Task<CategorizationResult> ResolveCategoryAsync(string userId, string rawDescription, CancellationToken cancellationToken)
    {
        var sanitized = SanitizedDescription.Create(rawDescription);

        foreach (var resolver in _resolvers)
        {
            var result = await resolver.ResolveAsync(userId, sanitized, cancellationToken);
            if (result != null)
            {
                return result;
            }
        }

        return new CategorizationResult(DefaultFallbackCategoryResolver.OthersCategoryId, CategorizationSource.Fallback);
    }
}
