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
    string MerchantName);

public class IngestTransactionCommandHandler : IIngestTransactionCommandHandler
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAccountBalanceRepository _accountBalanceRepository;
    private readonly ICategoryResolverPipeline _categoryResolverPipeline;
    private readonly IEventPublisher _eventPublisher;
    private readonly IUnitOfWork _unitOfWork;

    public IngestTransactionCommandHandler(
        ITransactionRepository transactionRepository,
        IAccountBalanceRepository accountBalanceRepository,
        ICategoryResolverPipeline categoryResolverPipeline,
        IEventPublisher eventPublisher,
        IUnitOfWork unitOfWork)
    {
        _transactionRepository = transactionRepository;
        _accountBalanceRepository = accountBalanceRepository;
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

        // Classificação automática de neutralidade para categorias e padrões conhecidos
        var transferCategoryId = Guid.Parse("11111111-1111-1111-1111-111111111002");
        var billPaymentCategoryId = Guid.Parse("11111111-1111-1111-1111-111111110801");
        var investmentsCategoryId = Guid.Parse("11111111-1111-1111-1111-111111110805");
        var descUpper = sanitizedDescription.CleanText.ToUpperInvariant();

        if (categorization.CategoryId == billPaymentCategoryId || descUpper.Contains("FATURA"))
        {
            transaction.MarkAsBillPayment();
        }

        if (categorization.CategoryId == transferCategoryId || 
            ((descUpper.Contains("JOSE HENRIQUE MARTINS DOTTA") || descUpper.Contains("JOSÉ HENRIQUE MARTINS DOTTA")) && !descUpper.Contains("WELLHUB")))
        {
            transaction.ToggleIgnoreInTotals(true);
        }
        else if (categorization.CategoryId == investmentsCategoryId || 
                 descUpper.Contains("NOSSA GRANA") || 
                 descUpper.Contains("DINHEIRO RETIRADO") || 
                 descUpper.Contains("DINHEIRO GUARDADO") ||
                 descUpper.Contains("COFRINHO"))
        {
            transaction.ToggleIgnoreInTotals(true);
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
