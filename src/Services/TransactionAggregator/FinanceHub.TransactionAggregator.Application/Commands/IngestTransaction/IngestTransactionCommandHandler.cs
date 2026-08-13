using System;
using System.Threading;
using System.Threading.Tasks;
using FinanceHub.Shared.Messaging.Events;
using FinanceHub.TransactionAggregator.Application.Interfaces;
using FinanceHub.TransactionAggregator.Application.Services.Categorization;
using FinanceHub.TransactionAggregator.Domain.Entities;
using FinanceHub.TransactionAggregator.Domain.ValueObjects;

namespace FinanceHub.TransactionAggregator.Application.Commands.IngestTransaction;

public record IngestTransactionCommand(
    string UserId,
    string InstitutionId,
    string AccountNumber,
    string BankTransactionId,
    decimal Amount,
    string Currency,
    TransactionType Type,
    string RawDescription,
    DateTime TransactionDateUtc,
    TransactionChannel Channel,
    string MerchantName);

public class IngestTransactionCommandHandler : IIngestTransactionCommandHandler
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAccountBalanceRepository _accountBalanceRepository;
    private readonly ICategoryResolverPipeline _categoryResolverPipeline;
    private readonly IEventPublisher _eventPublisher;

    public IngestTransactionCommandHandler(
        ITransactionRepository transactionRepository,
        IAccountBalanceRepository accountBalanceRepository,
        ICategoryResolverPipeline categoryResolverPipeline,
        IEventPublisher eventPublisher)
    {
        _transactionRepository = transactionRepository;
        _accountBalanceRepository = accountBalanceRepository;
        _categoryResolverPipeline = categoryResolverPipeline;
        _eventPublisher = eventPublisher;
    }

    public async Task<Guid> Handle(IngestTransactionCommand command, CancellationToken cancellationToken)
    {
        var accountInfo = new AccountIdentifier(command.InstitutionId, command.AccountNumber);
        var moneyAmount = new Money(command.Amount, command.Currency);
        var sanitizedDescription = SanitizedDescription.Create(command.RawDescription);

        var hash = TransactionHash.ComputeHash(
            command.InstitutionId,
            command.AccountNumber,
            command.BankTransactionId,
            command.Amount,
            command.TransactionDateUtc);

        if (await _transactionRepository.ExistsByHashAsync(hash, cancellationToken))
        {
            var existingId = await _transactionRepository.GetIdByHashAsync(hash, cancellationToken);
            return existingId ?? Guid.Empty;
        }

        var categorization = await _categoryResolverPipeline.ResolveCategoryAsync(
            command.UserId,
            command.RawDescription,
            cancellationToken);

        var bankDetails = new BankTransactionDetails(
            command.BankTransactionId,
            command.Channel,
            command.MerchantName);

        var creationParams = new CanonicalTransactionCreationParams(
            command.UserId,
            accountInfo,
            hash,
            moneyAmount,
            command.Type,
            sanitizedDescription,
            categorization.CategoryId,
            categorization.Source,
            command.TransactionDateUtc,
            bankDetails);

        var transaction = CanonicalTransaction.Create(creationParams);

        await _transactionRepository.AddAsync(transaction, cancellationToken);

        var balance = await _accountBalanceRepository.GetByUserAndAccountAsync(command.UserId, accountInfo, cancellationToken);
        if (balance == null)
        {
            balance = AccountBalance.Create(command.UserId, accountInfo, moneyAmount);
        }
        else
        {
            balance.ApplyTransaction(moneyAmount, command.Type);
        }

        await _accountBalanceRepository.AddOrUpdateAsync(balance, cancellationToken);

        await _eventPublisher.PublishAsync(new TransactionNormalized(
            TransactionId: transaction.Id,
            Source: command.InstitutionId,
            AccountId: command.AccountNumber,
            Amount: transaction.Amount.Amount,
            Currency: transaction.Amount.Currency,
            TransactionType: transaction.Type.ToString(),
            TransactionDate: transaction.TransactionDateUtc,
            CleanDescription: transaction.Description.CleanText,
            HashDeduplicacao: transaction.Hash.Value,
            ProcessedAtUtc: DateTime.UtcNow), cancellationToken);

        return transaction.Id;
    }
}
