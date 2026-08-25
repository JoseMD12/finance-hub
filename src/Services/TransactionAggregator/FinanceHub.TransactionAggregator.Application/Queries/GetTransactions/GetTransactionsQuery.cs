using FinanceHub.TransactionAggregator.Application.DTOs;

namespace FinanceHub.TransactionAggregator.Application.Queries.GetTransactions;

public record GetTransactionsQuery(TransactionFilterDto Filter);
