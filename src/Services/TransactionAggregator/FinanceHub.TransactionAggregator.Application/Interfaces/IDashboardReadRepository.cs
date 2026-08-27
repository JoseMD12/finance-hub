using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FinanceHub.TransactionAggregator.Application.DTOs;

namespace FinanceHub.TransactionAggregator.Application.Interfaces;

/// <summary>
/// Porta de leitura exclusiva do Dashboard, para as agregações que o repositório de transações
/// não faz. Toda consulta agrupa no PostgreSQL — nada de materializar transações em memória
/// para somar em C#.
/// </summary>
public interface IDashboardReadRepository
{
    /// <summary>
    /// Despesas por categoria no período, da maior para a menor, limitadas a
    /// <paramref name="topCount"/>. A cauda é colapsada numa fatia "Outras", para que o gráfico
    /// não vire uma lista ilegível de categorias irrelevantes.
    /// </summary>
    Task<IReadOnlyList<CategoryExpenseDto>> GetCategoryExpenseBreakdownAsync(
        DashboardFilterDto filter,
        int topCount,
        CancellationToken cancellationToken);

    /// <summary>
    /// Receita, despesa e resultado dos últimos <paramref name="monthsBack"/> meses.
    /// Ignora o filtro de período: a série de evolução é justamente o contexto em volta dele.
    /// </summary>
    Task<IReadOnlyList<MonthlyCashFlowPointDto>> GetMonthlyCashFlowAsync(
        string userId,
        int monthsBack,
        CancellationToken cancellationToken);

    /// <summary>Saldos por conta, com dados de crédito quando a conta é cartão.</summary>
    Task<IReadOnlyList<InstitutionBalanceDto>> GetInstitutionBalancesAsync(
        string userId,
        CancellationToken cancellationToken);
}
