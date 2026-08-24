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

        // FASE 1: Pareamento de Transferências Próprias (Contas distintas, mesmo valor, <= 96h)
        foreach (var debit in debits)
        {
            if (matchedDebits.Contains(debit.Id))
            {
                continue;
            }

            var compatibleCredit = credits.FirstOrDefault(credit =>
                !matchedCredits.Contains(credit.Id) &&
                credit.Amount.Amount == debit.Amount.Amount &&
                credit.Amount.Currency == debit.Amount.Currency &&
                (credit.AccountInfo.AccountId != debit.AccountInfo.AccountId || credit.AccountInfo.InstitutionId != debit.AccountInfo.InstitutionId) &&
                Math.Abs((credit.TransactionDateUtc - debit.TransactionDateUtc).TotalMilliseconds) <= MaxInternalTransferWindow.TotalMilliseconds);

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

        // FASE 2: Pareamento Recíproco com Terceiros (Repasses / Empréstimos: nome comum ou valor exato em <= 72h)
        foreach (var debit in debits)
        {
            if (matchedDebits.Contains(debit.Id))
            {
                continue;
            }

            var debitName = ExtractPersonName(debit.Description.CleanText);

            // Prioriza correspondência com mesmo nome de terceiro se disponível
            var compatibleCredit = credits.FirstOrDefault(credit =>
            {
                if (matchedCredits.Contains(credit.Id))
                {
                    return false;
                }

                if (credit.Amount.Amount != debit.Amount.Amount || credit.Amount.Currency != debit.Amount.Currency)
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

                // Se não tem nome de terceiro e for a mesma conta, não pareia como repasse de terceiro
                if (credit.AccountInfo.AccountId == debit.AccountInfo.AccountId && credit.AccountInfo.InstitutionId == debit.AccountInfo.InstitutionId)
                {
                    return false;
                }

                return true;
            });

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

        // FASE 3: Detecção de Dinheiro de Trânsito / Boleto Espelho (Entrada Pix/Transf seguida de saída/boleto com delta <= R$ 5,00 em <= 24h)
        foreach (var credit in credits)
        {
            if (matchedCredits.Contains(credit.Id))
            {
                continue;
            }

            var compatibleDebit = debits.FirstOrDefault(debit =>
            {
                if (matchedDebits.Contains(debit.Id))
                {
                    return false;
                }

                if (credit.Amount.Currency != debit.Amount.Currency)
                {
                    return false;
                }

                var timeDiff = (debit.TransactionDateUtc - credit.TransactionDateUtc).TotalMilliseconds;
                // Saída no mesmo dia ou até 24h após a entrada
                if (timeDiff < 0 || timeDiff > MaxTransitMoneyWindow.TotalMilliseconds)
                {
                    return false;
                }

                var amountDiff = Math.Abs(credit.Amount.Amount - debit.Amount.Amount);
                return amountDiff <= MaxTransitMoneyDifferenceBrl;
            });

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

        if (modifiedTransactions.Count > 0)
        {
            await _repository.UpdateRangeAsync(modifiedTransactions, cancellationToken);
        }

        return (matchedDebits.Count + matchedCredits.Count) / 2;
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
