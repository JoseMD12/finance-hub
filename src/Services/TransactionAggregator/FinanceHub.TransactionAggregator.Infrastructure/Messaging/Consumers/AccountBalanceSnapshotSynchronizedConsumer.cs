using System;
using System.Linq;
using System.Threading.Tasks;
using FinanceHub.Shared.Messaging.Events;
using FinanceHub.TransactionAggregator.Domain.Entities;
using FinanceHub.TransactionAggregator.Domain.ValueObjects;
using FinanceHub.TransactionAggregator.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinanceHub.TransactionAggregator.Infrastructure.Messaging.Consumers;

public class AccountBalanceSnapshotSynchronizedConsumer : IConsumer<AccountBalanceSnapshotSynchronized>
{
    private readonly TransactionAggregatorDbContext _dbContext;
    private readonly ILogger<AccountBalanceSnapshotSynchronizedConsumer> _logger;

    public AccountBalanceSnapshotSynchronizedConsumer(
        TransactionAggregatorDbContext dbContext,
        ILogger<AccountBalanceSnapshotSynchronizedConsumer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AccountBalanceSnapshotSynchronized> context)
    {
        var msg = context.Message;
        if (string.IsNullOrWhiteSpace(msg.UserId) || msg.Accounts == null || msg.Accounts.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Sincronizando snapshot oficial de saldos Open Finance para UserId: {UserId} ({Count} contas)",
            msg.UserId, msg.Accounts.Count);

        var existingBalances = await _dbContext.AccountBalances
            .Where(b => b.UserId == msg.UserId)
            .ToListAsync(context.CancellationToken);

        // Remover saldos dummy ou desatualizados que não estejam mais nas contas oficiais ativas
        var activeAccountIds = msg.Accounts.Select(a => a.AccountId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var obsoleteBalances = existingBalances
            .Where(b => !activeAccountIds.Contains(b.AccountInfo.AccountId))
            .ToList();

        if (obsoleteBalances.Count > 0)
        {
            _dbContext.AccountBalances.RemoveRange(obsoleteBalances);
        }

        foreach (var accountItem in msg.Accounts)
        {
            var existing = existingBalances.FirstOrDefault(b =>
                string.Equals(b.AccountInfo.AccountId, accountItem.AccountId, StringComparison.OrdinalIgnoreCase));

            var money = new Money(accountItem.CurrentBalance, accountItem.Currency ?? "BRL");
            var accountInfo = new AccountIdentifier(accountItem.InstitutionId, accountItem.AccountId);

            var creditInfo = BuildCreditInfo(accountItem);

            if (existing == null)
            {
                var newBalance = AccountBalance.Create(msg.UserId, accountInfo, money);
                newBalance.SynchronizeWithBankSnapshot(money, accountItem.SnapshotAtUtc);
                newBalance.SynchronizeCreditData(creditInfo ?? CreditAccountInfo.None);
                await _dbContext.AccountBalances.AddAsync(newBalance, context.CancellationToken);
            }
            else
            {
                existing.SynchronizeWithBankSnapshot(money, accountItem.SnapshotAtUtc);

                // Mensagem sem informação de crédito preserva o que já está gravado, em vez de
                // zerar limites e vencimentos de contas já sincronizadas.
                if (creditInfo is not null)
                {
                    existing.SynchronizeCreditData(creditInfo);
                }

                _dbContext.AccountBalances.Update(existing);
            }
        }

        await _dbContext.SaveChangesAsync(context.CancellationToken);
        _logger.LogInformation("Snapshot oficial de saldos atualizado com sucesso para UserId: {UserId}", msg.UserId);
    }

    /// <summary>
    /// Devolve <c>null</c> quando o publisher não informou nada sobre crédito, sinalizando ao
    /// chamador que o estado atual deve ser preservado.
    /// </summary>
    private static CreditAccountInfo? BuildCreditInfo(AccountBalanceSnapshotItem accountItem)
    {
        if (accountItem.IsCreditCard is null)
        {
            return null;
        }

        if (accountItem.IsCreditCard == false)
        {
            return CreditAccountInfo.None;
        }

        return new CreditAccountInfo(
            isCreditCard: true,
            creditLimit: accountItem.CreditLimit,
            availableCreditLimit: accountItem.AvailableCreditLimit,
            invoiceDueDateUtc: accountItem.InvoiceDueDateUtc,
            invoiceClosingDateUtc: accountItem.InvoiceClosingDateUtc);
    }
}
