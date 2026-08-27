using System;
using System.Threading;
using System.Threading.Tasks;
using FinanceHub.Shared.Messaging.Events;
using FinanceHub.TransactionAggregator.Application.Interfaces;
using FinanceHub.TransactionAggregator.Application.Services.Categorization;
using FinanceHub.TransactionAggregator.Domain.Entities;
using FinanceHub.TransactionAggregator.Domain.Services;
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
    string MerchantName,
    DateTime? InvoiceDueDateUtc = null,
    int? CurrentInstallment = null,
    int? TotalInstallments = null);

public class IngestTransactionCommandHandler : IIngestTransactionCommandHandler
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAccountBalanceRepository _accountBalanceRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ICategoryResolverPipeline _categoryResolverPipeline;
    private readonly IEventPublisher _eventPublisher;
    private readonly IUnitOfWork _unitOfWork;

    public IngestTransactionCommandHandler(
        ITransactionRepository transactionRepository,
        IAccountBalanceRepository accountBalanceRepository,
        ICategoryRepository categoryRepository,
        ICategoryResolverPipeline categoryResolverPipeline,
        IEventPublisher eventPublisher,
        IUnitOfWork unitOfWork)
    {
        _transactionRepository = transactionRepository;
        _accountBalanceRepository = accountBalanceRepository;
        _categoryRepository = categoryRepository;
        _categoryResolverPipeline = categoryResolverPipeline;
        _eventPublisher = eventPublisher;
        _unitOfWork = unitOfWork;
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

        var existingId = await _transactionRepository.GetIdByHashAsync(hash, cancellationToken);
        if (existingId != null)
        {
            return existingId.Value;
        }

        var categorization = await _categoryResolverPipeline.ResolveCategoryAsync(
            command.UserId,
            command.RawDescription,
            cancellationToken);

        var channel = TransactionChannelDetector.ResolveChannel(command.Channel, command.RawDescription);

        var bankDetails = new BankTransactionDetails(
            command.BankTransactionId,
            channel,
            command.MerchantName,
            command.InvoiceDueDateUtc,
            command.CurrentInstallment,
            command.TotalInstallments);

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

        // A natureza econômica vem da categoria resolvida pelo pipeline, e dela deriva a
        // neutralidade nos totais. Nenhum casamento de texto com dado de usuário específico:
        // o conhecimento de mercado vive nos datasets de categorias e merchants.
        var nature = await _categoryRepository.GetNatureByCategoryIdAsync(
            categorization.CategoryId, cancellationToken);

        transaction.ApplyNature(nature);

        if (TransactionBillPaymentDetector.IsBillPayment(sanitizedDescription.CleanText))
        {
            transaction.MarkAsBillPayment();
        }

        await _transactionRepository.AddAsync(transaction, cancellationToken);

        await _unitOfWork.CommitAsync(cancellationToken);

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
