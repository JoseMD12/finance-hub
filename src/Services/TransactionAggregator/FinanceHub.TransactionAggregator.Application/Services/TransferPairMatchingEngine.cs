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
    private static readonly TimeSpan MaxInternalTransferWindow = TimeSpan.FromHours(96);
    private static readonly TimeSpan MaxThirdPartyReciprocalWindow = TimeSpan.FromHours(72);
    private static readonly TimeSpan MaxTransitMoneyWindow = TimeSpan.FromHours(24);
    private const decimal MaxTransitMoneyDifferenceBrl = 2.00m;

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

        MatchInternalTransfers(userId, debits, credits, matchedDebits, matchedCredits, modifiedTransactions);
        MatchThirdPartyTransfers(userId, debits, credits, matchedDebits, matchedCredits, modifiedTransactions);
        MatchTransitMoney(userId, debits, credits, matchedDebits, matchedCredits, modifiedTransactions);

        if (modifiedTransactions.Count > 0)
        {
            await _repository.UpdateRangeAsync(modifiedTransactions, cancellationToken);
        }

        return (matchedDebits.Count + matchedCredits.Count) / 2;
    }

    private void MatchInternalTransfers(
        string userId,
        List<CanonicalTransaction> debits,
        List<CanonicalTransaction> credits,
        HashSet<Guid> matchedDebits,
        HashSet<Guid> matchedCredits,
        List<CanonicalTransaction> modifiedTransactions)
    {
        foreach (var debit in debits)
        {
            if (matchedDebits.Contains(debit.Id))
            {
                continue;
            }

            var compatibleCredit = credits.FirstOrDefault(credit => IsInternalTransferMatch(debit, credit, matchedCredits));

            if (compatibleCredit != null)
            {
                debit.MarkAsInternalTransfer(compatibleCredit.Id);
                compatibleCredit.MarkAsInternalTransfer(debit.Id);

                matchedDebits.Add(debit.Id);
                matchedCredits.Add(compatibleCredit.Id);

                modifiedTransactions.Add(debit);
                modifiedTransactions.Add(compatibleCredit);

                _logger.LogInformation(
                    "Transferencia interna entre contas pareada com sucesso para usuario {UserId}. Debito={DebitId}, Credito={CreditId}, Valor={Amount} {Currency}",
                    userId, debit.Id, compatibleCredit.Id, debit.Amount.Amount, debit.Amount.Currency);
            }
        }
    }

    private static bool IsInternalTransferMatch(CanonicalTransaction debit, CanonicalTransaction credit, HashSet<Guid> matchedCredits)
    {
        return !matchedCredits.Contains(credit.Id) &&
               credit.Amount.Amount == debit.Amount.Amount &&
               credit.Amount.Currency == debit.Amount.Currency &&
               (credit.AccountInfo.AccountId != debit.AccountInfo.AccountId || credit.AccountInfo.InstitutionId != debit.AccountInfo.InstitutionId) &&
               Math.Abs((credit.TransactionDateUtc - debit.TransactionDateUtc).TotalMilliseconds) <= MaxInternalTransferWindow.TotalMilliseconds;
    }

    private void MatchThirdPartyTransfers(
        string userId,
        List<CanonicalTransaction> debits,
        List<CanonicalTransaction> credits,
        HashSet<Guid> matchedDebits,
        HashSet<Guid> matchedCredits,
        List<CanonicalTransaction> modifiedTransactions)
    {
        foreach (var debit in debits)
        {
            if (matchedDebits.Contains(debit.Id))
            {
                continue;
            }

            var debitName = ExtractPersonName(debit.Description.CleanText);
            var compatibleCredit = credits.FirstOrDefault(credit => IsThirdPartyMatch(debit, credit, debitName, matchedCredits));

            if (compatibleCredit != null)
            {
                debit.MarkAsInternalTransfer(compatibleCredit.Id);
                compatibleCredit.MarkAsInternalTransfer(debit.Id);

                matchedDebits.Add(debit.Id);
                matchedCredits.Add(compatibleCredit.Id);

                modifiedTransactions.Add(debit);
                modifiedTransactions.Add(compatibleCredit);

                _logger.LogInformation(
                    "Repasse com terceiro pareado com sucesso para usuario {UserId}. Debito={DebitId}, Credito={CreditId}, Valor={Amount} {Currency}",
                    userId, debit.Id, compatibleCredit.Id, debit.Amount.Amount, debit.Amount.Currency);
            }
        }
    }

    private static bool IsThirdPartyMatch(CanonicalTransaction debit, CanonicalTransaction credit, string debitName, HashSet<Guid> matchedCredits)
    {
        if (matchedCredits.Contains(credit.Id) || credit.Amount.Amount != debit.Amount.Amount || credit.Amount.Currency != debit.Amount.Currency)
        {
            return false;
        }

        if (Math.Abs((credit.TransactionDateUtc - debit.TransactionDateUtc).TotalMilliseconds) > MaxThirdPartyReciprocalWindow.TotalMilliseconds)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(debitName))
        {
            var creditName = ExtractPersonName(credit.Description.CleanText);
            if (!string.IsNullOrWhiteSpace(creditName) && string.Equals(debitName, creditName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return credit.AccountInfo.AccountId != debit.AccountInfo.AccountId || credit.AccountInfo.InstitutionId != debit.AccountInfo.InstitutionId;
    }

    private void MatchTransitMoney(
        string userId,
        List<CanonicalTransaction> debits,
        List<CanonicalTransaction> credits,
        HashSet<Guid> matchedDebits,
        HashSet<Guid> matchedCredits,
        List<CanonicalTransaction> modifiedTransactions)
    {
        foreach (var credit in credits)
        {
            if (matchedCredits.Contains(credit.Id))
            {
                continue;
            }

            var compatibleDebit = debits.FirstOrDefault(debit => IsTransitMoneyMatch(credit, debit, matchedDebits));

            if (compatibleDebit != null)
            {
                credit.MarkAsTransitMoney(compatibleDebit.Id);
                compatibleDebit.MarkAsTransitMoney(credit.Id);

                matchedCredits.Add(credit.Id);
                matchedDebits.Add(compatibleDebit.Id);

                modifiedTransactions.Add(credit);
                modifiedTransactions.Add(compatibleDebit);

                _logger.LogInformation(
                    "Dinheiro de transito / boleto espelho pareado com sucesso para usuario {UserId}. Entrada={CreditId}, Saida={DebitId}, Delta={Delta} {Currency}",
                    userId, credit.Id, compatibleDebit.Id, Math.Abs(credit.Amount.Amount - compatibleDebit.Amount.Amount), credit.Amount.Currency);
            }
        }
    }

    private static bool IsTransitMoneyMatch(CanonicalTransaction credit, CanonicalTransaction debit, HashSet<Guid> matchedDebits)
    {
        if (matchedDebits.Contains(debit.Id) || credit.Amount.Currency != debit.Amount.Currency)
        {
            return false;
        }

        var timeDiff = (debit.TransactionDateUtc - credit.TransactionDateUtc).TotalMilliseconds;
        if (timeDiff < 0 || timeDiff > MaxTransitMoneyWindow.TotalMilliseconds)
        {
            return false;
        }

        var amountDiff = Math.Abs(credit.Amount.Amount - debit.Amount.Amount);
        return amountDiff <= MaxTransitMoneyDifferenceBrl;
    }

    private static string ExtractPersonName(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return string.Empty;
        }

        var cleaned = description.ToUpperInvariant()
            .Replace("PIX ENVIADO", "")
            .Replace("PIX RECEBIDO", "")
            .Replace("PIX TRANSF", "")
            .Replace("TRANSF", "")
            .Replace("TED", "")
            .Replace("DOC", "")
            .Trim('-', ' ', ':');

        return cleaned.Trim();
    }
}
