using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FinanceHub.TransactionAggregator.Application.Interfaces;
using FinanceHub.TransactionAggregator.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace FinanceHub.TransactionAggregator.Application.Services;

public class TransferPairMatchingEngine : ITransferPairMatchingEngine
{
    private static readonly TimeSpan MaxMatchingWindow = TimeSpan.FromHours(96);
    private readonly ITransactionRepository _repository;
    private readonly ILogger<TransferPairMatchingEngine> _logger;

    public TransferPairMatchingEngine(
        ITransactionRepository repository,
        ILogger<TransferPairMatchingEngine> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<int> MatchAndPairAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return 0;
        }

        var toUtc = DateTime.UtcNow;
        var fromUtc = toUtc.AddDays(-365); // Janela histórica de busca

        var candidates = (await _repository.GetUnpairedTransfersCandidateAsync(userId, fromUtc, toUtc, cancellationToken))
            .ToList();

        if (candidates.Count < 2)
        {
            return 0;
        }

        var debits = candidates
            .Where(t => t.Type == TransactionType.Debit && t.PairedTransactionId == null)
            .OrderBy(t => t.TransactionDateUtc)
            .ToList();

        var credits = candidates
            .Where(t => t.Type == TransactionType.Credit && t.PairedTransactionId == null)
            .OrderBy(t => t.TransactionDateUtc)
            .ToList();

        var matchedDebits = new HashSet<Guid>();
        var matchedCredits = new HashSet<Guid>();
        var modifiedTransactions = new List<CanonicalTransaction>();

        foreach (var debit in debits)
        {
            if (matchedDebits.Contains(debit.Id))
            {
                continue;
            }

            // Buscar melhor crédito correspondente
            var compatibleCredit = credits.FirstOrDefault(credit =>
                !matchedCredits.Contains(credit.Id) &&
                credit.Amount.Amount == debit.Amount.Amount &&
                credit.Amount.Currency == debit.Amount.Currency &&
                (credit.AccountInfo.AccountId != debit.AccountInfo.AccountId || credit.AccountInfo.InstitutionId != debit.AccountInfo.InstitutionId) &&
                Math.Abs((credit.TransactionDateUtc - debit.TransactionDateUtc).TotalMilliseconds) <= MaxMatchingWindow.TotalMilliseconds);

            if (compatibleCredit != null)
            {
                debit.MarkAsInternalTransfer(compatibleCredit.Id);
                compatibleCredit.MarkAsInternalTransfer(debit.Id);

                matchedDebits.Add(debit.Id);
                matchedCredits.Add(compatibleCredit.Id);

                modifiedTransactions.Add(debit);
                modifiedTransactions.Add(compatibleCredit);

                _logger.LogInformation(
                    "Transferencia interna pareada com sucesso para usuario {UserId}. Debito={DebitId}, Credito={CreditId}, Valor={Amount} {Currency}",
                    userId, debit.Id, compatibleCredit.Id, debit.Amount.Amount, debit.Amount.Currency);
            }
        }

        if (modifiedTransactions.Count > 0)
        {
            await _repository.UpdateRangeAsync(modifiedTransactions, cancellationToken);
        }

        return matchedDebits.Count;
    }
}
