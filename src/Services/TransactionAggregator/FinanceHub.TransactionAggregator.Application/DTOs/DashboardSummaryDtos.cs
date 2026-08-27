using System;
using System.Collections.Generic;

namespace FinanceHub.TransactionAggregator.Application.DTOs;

/// <summary>
/// Filtro do Dashboard. Espelha o subconjunto de <see cref="TransactionFilterDto"/> que faz
/// sentido para uma visão agregada — sem paginação, que é conceito de listagem.
/// </summary>
public record DashboardFilterDto(
    string UserId,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    string? InstitutionId = null,
    bool IncludeIgnoredInTotals = false);

/// <summary>
/// Saldo de uma conta, com os dados de crédito quando se trata de cartão.
/// O nome de exibição e o logotipo da instituição são resolvidos no frontend a partir de
/// <c>InstitutionId</c>, para não duplicar catálogo de bancos no backend.
/// </summary>
public record InstitutionBalanceDto(
    string InstitutionId,
    string AccountNumber,
    decimal BalanceBrl,
    string Currency,
    bool IsCreditCard,
    decimal? CreditLimit,
    decimal? AvailableCreditLimit,
    decimal? UsedCreditLimit,
    DateTime? InvoiceDueDateUtc,
    DateTime LastUpdatedAtUtc);

/// <summary>
/// Despesa consolidada por categoria no período. <c>Percentage</c> é a fatia do total de
/// despesas, já calculada no servidor para que a interface não precise refazer a conta.
/// </summary>
public record CategoryExpenseDto(
    Guid CategoryId,
    string CategoryName,
    string ColorToken,
    string IconKey,
    decimal AmountBrl,
    decimal Percentage);

/// <summary>Receita, despesa e resultado de um mês, para a série de evolução.</summary>
public record MonthlyCashFlowPointDto(
    int Year,
    int Month,
    decimal IncomeBrl,
    decimal ExpenseBrl,
    decimal NetBrl);

/// <summary>
/// Resposta agregada do Dashboard.
///
/// <c>Summary</c> e <c>RecentTransactions</c> vêm do mesmo
/// <c>ITransactionRepository.QueryPagedByFilterAsync</c> que alimenta a tela de Transações —
/// é o que garante que as duas telas nunca mostrem números diferentes para o mesmo período.
/// </summary>
public record DashboardSummaryDto(
    string UserId,
    TransactionSummaryDto Summary,
    IReadOnlyList<InstitutionBalanceDto> InstitutionBalances,
    IReadOnlyList<CategoryExpenseDto> CategoryExpenses,
    IReadOnlyList<MonthlyCashFlowPointDto> MonthlyCashFlow,
    IReadOnlyList<TransactionDto> RecentTransactions,
    DateTime GeneratedAtUtc);
