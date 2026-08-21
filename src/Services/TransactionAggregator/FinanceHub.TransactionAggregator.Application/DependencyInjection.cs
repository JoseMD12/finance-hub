using FinanceHub.TransactionAggregator.Application.Commands.CategorizeTransaction;
using FinanceHub.TransactionAggregator.Application.Commands.IngestTransaction;
using FinanceHub.TransactionAggregator.Application.Queries.GetCategories;
using FinanceHub.TransactionAggregator.Application.Queries.GetConsolidatedBalance;
using FinanceHub.TransactionAggregator.Application.Queries.GetTransactions;
using FinanceHub.TransactionAggregator.Application.Services.Categorization;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceHub.TransactionAggregator.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddTransactionAggregatorApplicationServices(this IServiceCollection services)
    {
        // Categorization Resolvers Pipeline
        services.AddScoped<ICategoryResolver, UserCustomRuleCategoryResolver>();
        services.AddScoped<ICategoryResolver, GlobalPatternCategoryResolver>();
        services.AddScoped<ICategoryResolver, DefaultFallbackCategoryResolver>();
        services.AddScoped<ICategoryResolverPipeline, CategoryResolverPipeline>();

        // Command & Query Handlers
        services.AddScoped<IIngestTransactionCommandHandler, IngestTransactionCommandHandler>();
        services.AddScoped<ICategorizeTransactionCommandHandler, CategorizeTransactionCommandHandler>();
        services.AddScoped<IGetTransactionsQueryHandler, GetTransactionsQueryHandler>();
        services.AddScoped<IGetCategoriesQueryHandler, GetCategoriesQueryHandler>();
        services.AddScoped<IGetConsolidatedBalanceQueryHandler, GetConsolidatedBalanceQueryHandler>();

        return services;
    }
}
