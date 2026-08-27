using System;
using System.Threading;
using System.Threading.Tasks;
using FinanceHub.TransactionAggregator.Application.Commands.IngestTransaction;
using FinanceHub.TransactionAggregator.Application.Interfaces;
using FinanceHub.TransactionAggregator.Application.Services.Categorization;
using FinanceHub.TransactionAggregator.Domain.Entities;
using FinanceHub.TransactionAggregator.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FinanceHub.Tests.Services.TransactionAggregator.Application;

/// <summary>
/// Garante que o vencimento da fatura e as parcelas atravessam o handler de ingestão
/// e chegam persistidos em <c>BankTransactionDetails</c>, em vez de se perderem no caminho.
/// </summary>
public class IngestTransactionCreditDataTests
{
    private static readonly DateTime DueDate = new(2026, 9, 5, 0, 0, 0, DateTimeKind.Utc);

    private readonly ITransactionRepository _txRepo = Substitute.For<ITransactionRepository>();
    private readonly IAccountBalanceRepository _balanceRepo = Substitute.For<IAccountBalanceRepository>();
    private readonly ICategoryResolverPipeline _pipeline = Substitute.For<ICategoryResolverPipeline>();
    private readonly IEventPublisher _eventPublisher = Substitute.For<IEventPublisher>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IngestTransactionCommandHandler _handler;

    public IngestTransactionCreditDataTests()
    {
        _pipeline
            .ResolveCategoryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new CategorizationResult(Guid.NewGuid(), CategorizationSource.GlobalRule));

        _txRepo.GetIdByHashAsync(Arg.Any<TransactionHash>(), Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        _handler = new IngestTransactionCommandHandler(
            _txRepo, _balanceRepo, _pipeline, _eventPublisher, _unitOfWork);
    }

    [Fact]
    public async Task Handle_WhenCommandCarriesInvoiceData_ShouldPersistItOnBankDetails()
    {
        var command = BuildCommand(dueDate: DueDate, currentInstallment: 2, totalInstallments: 6);

        await _handler.Handle(command, CancellationToken.None);

        await _txRepo.Received(1).AddAsync(
            Arg.Is<CanonicalTransaction>(t =>
                t.BankDetails.InvoiceDueDateUtc == DueDate &&
                t.BankDetails.CurrentInstallment == 2 &&
                t.BankDetails.TotalInstallments == 6 &&
                t.BankDetails.IsInstallment),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCommandHasNoInvoiceData_ShouldPersistNullsWithoutThrowing()
    {
        var command = BuildCommand();

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().NotThrowAsync();
        await _txRepo.Received(1).AddAsync(
            Arg.Is<CanonicalTransaction>(t =>
                t.BankDetails.InvoiceDueDateUtc == null &&
                t.BankDetails.CurrentInstallment == null &&
                t.BankDetails.TotalInstallments == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTransactionIsDuplicated_ShouldNotPersistAgain()
    {
        var existingId = Guid.NewGuid();
        _txRepo.GetIdByHashAsync(Arg.Any<TransactionHash>(), Arg.Any<CancellationToken>())
            .Returns(existingId);

        var resultId = await _handler.Handle(
            BuildCommand(dueDate: DueDate), CancellationToken.None);

        resultId.Should().Be(existingId);
        await _txRepo.DidNotReceive().AddAsync(
            Arg.Any<CanonicalTransaction>(), Arg.Any<CancellationToken>());
    }

    private static IngestTransactionCommand BuildCommand(
        DateTime? dueDate = null,
        int? currentInstallment = null,
        int? totalInstallments = null)
        => new(
            UserId: "user-1",
            InstitutionId: "itau",
            AccountNumber: "acc-card",
            BankTransactionId: "tx-card-1",
            Amount: 250.00m,
            Currency: "BRL",
            Type: TransactionType.Debit,
            RawDescription: "Supermercado",
            TransactionDateUtc: new DateTime(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc),
            Channel: TransactionChannel.CreditCard,
            MerchantName: "Supermercado",
            InvoiceDueDateUtc: dueDate,
            CurrentInstallment: currentInstallment,
            TotalInstallments: totalInstallments);
}
