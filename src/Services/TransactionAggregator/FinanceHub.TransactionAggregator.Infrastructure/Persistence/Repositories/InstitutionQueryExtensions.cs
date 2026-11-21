using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FinanceHub.TransactionAggregator.Domain.Constants;
using FinanceHub.TransactionAggregator.Domain.Entities;
using FinanceHub.TransactionAggregator.Domain.Exceptions;
using FinanceHub.TransactionAggregator.Domain.ValueObjects;

namespace FinanceHub.TransactionAggregator.Infrastructure.Persistence.Repositories;

/// <summary>
/// Filtro de instituição compartilhado por todas as leituras de transações. Montado como cadeia
/// de <c>OrElse</c> em vez de <c>keywords.Any(...)</c> porque essa forma é traduzida por qualquer
/// provider; o <c>Any</c> sobre lista local com owned type falha fora do Npgsql.
/// </summary>
internal static class InstitutionQueryExtensions
{
    public static IQueryable<CanonicalTransaction> WhereInstitution(
        this IQueryable<CanonicalTransaction> query,
        string? institutionId)
    {
        if (string.IsNullOrWhiteSpace(institutionId))
        {
            return query;
        }

        return query.Where(BuildInstitutionFilterExpression(BankAliases.GetKeywordsFor(institutionId)));
    }

    private static Expression<Func<CanonicalTransaction, bool>> BuildInstitutionFilterExpression(IReadOnlyList<string> keywords)
    {
        var parameter = Expression.Parameter(typeof(CanonicalTransaction), "t");
        var accountInfoProperty = Expression.Property(parameter, nameof(CanonicalTransaction.AccountInfo));
        var institutionIdProperty = Expression.Property(accountInfoProperty, nameof(AccountIdentifier.InstitutionId));

        var toLowerMethod = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)
            ?? throw new MethodReflectionFailedDomainException("string.ToLower");
        var containsMethod = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])
            ?? throw new MethodReflectionFailedDomainException("string.Contains");

        var lowerInstitutionId = Expression.Call(institutionIdProperty, toLowerMethod);

        Expression? combined = null;

        foreach (var keyword in keywords)
        {
            var keywordConstant = Expression.Constant(keyword.ToLowerInvariant());
            var containsExpression = Expression.Call(lowerInstitutionId, containsMethod, keywordConstant);

            combined = combined == null
                ? containsExpression
                : Expression.OrElse(combined, containsExpression);
        }

        return Expression.Lambda<Func<CanonicalTransaction, bool>>(combined ?? Expression.Constant(true), parameter);
    }
}
