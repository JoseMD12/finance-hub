using System;
using System.Threading;
using System.Threading.Tasks;
using FinanceHub.Shared.Messaging.Events;
using FinanceHub.TransactionAggregator.Application.Commands.IngestTransaction;
using FinanceHub.TransactionAggregator.Application.Interfaces;
using FinanceHub.TransactionAggregator.Infrastructure.Messaging.Consumers;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace FinanceHub.Tests.Services.TransactionAggregator.Infrastructure;

/// <summary>
/// O evento <c>InvoiceItemIngested</c> sempre carregou vencimento da fatura e parcelas,
/// mas ambos os consumers montavam o <c>IngestTransactionCommand</c> sem esses campos —
/// então o Aggregator nunca soube que faturas vencem. Estes testes travam essa regressão
/// nos dois caminhos: o consumer de item único e o de lote, que é o caminho quente do sync.
/// </summary>
public class CreditInvoiceDataPropagationTests
{
    private static readonly DateTime DueDate = new(2026, 9, 5, 0, 0, 0, DateTimeKind.Utc);

    private readonly IIngestTransactionCommandHandler _handler = Substitute.For<IIngestTransactionCommandHandler>();
    private readonly ITransferPairMatchingEngine _matchingEngine = Substitute.For<ITransferPairMatchingEngine>();

    public CreditInvoiceDataPropagationTests()
    {
        _handler.Handle(Arg.Any<IngestTransactionCommand>(), Arg.Any<CancellationToken>())
            .Returns(Guid.NewGuid());
    }

    // ─── Consumer de item único ────────────────────────────────────────────────

    [Fact]
    public async Task InvoiceItemIngestedConsumer_ShouldForwardDueDateAndInstallmentsToCommand()
    {
        var consumer = new InvoiceItemIngestedConsumer(
            _handler,
            NullLogger<InvoiceItemIngestedConsumer>.Instance);

        var context = BuildContext(BuildInvoiceItem(currentInstallment: 2, totalInstallments: 6));

        await consumer.Consume(context);

        await _handler.Received(1).Handle(
            Arg.Is<IngestTransactionCommand>(c =>
                c.InvoiceDueDateUtc == DueDate &&
                c.CurrentInstallment == 2 &&
                c.TotalInstallments == 6),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvoiceItemIngestedConsumer_WhenCreditDataAbsent_ShouldForwardNullsWithoutThrowing()
    {
        var consumer = new InvoiceItemIngestedConsumer(
            _handler,
            NullLogger<InvoiceItemIngestedConsumer>.Instance);

        var context = BuildContext(BuildInvoiceItem(
            dueDate: null,
            currentInstallment: null,
            totalInstallments: null,
            omitDueDate: true));

        await consumer.Consume(context);

        await _handler.Received(1).Handle(
            Arg.Is<IngestTransactionCommand>(c =>
                c.InvoiceDueDateUtc == null &&
                c.CurrentInstallment == null &&
                c.TotalInstallments == null),
            Arg.Any<CancellationToken>());
    }

    // ─── Consumer de lote (caminho quente do sync) ─────────────────────────────

    [Fact]
    public async Task TransactionsBatchIngestedConsumer_ShouldForwardInvoiceDataForCardTransactions()
    {
        var consumer = new TransactionsBatchIngestedConsumer(
            _handler,
            _matchingEngine,
            NullLogger<TransactionsBatchIngestedConsumer>.Instance);

        var batch = new TransactionsBatchIngested(
            BatchId: Guid.NewGuid(),
            UserId: "user-01",
            ChunkIndex: 0,
            TotalChunks: 1,
            CheckingTransactions: Array.Empty<TransactionIngested>(),
            CardTransactions: new[] { BuildInvoiceItem(currentInstallment: 3, totalInstallments: 12) },
            OccurredAtUtc: DateTime.UtcNow);

        var context = Substitute.For<ConsumeContext<TransactionsBatchIngested>>();
        context.Message.Returns(batch);
        context.CancellationToken.Returns(CancellationToken.None);

        await consumer.Consume(context);

        await _handler.Received(1).Handle(
            Arg.Is<IngestTransactionCommand>(c =>
                c.InvoiceDueDateUtc == DueDate &&
                c.CurrentInstallment == 3 &&
                c.TotalInstallments == 12),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TransactionsBatchIngestedConsumer_CheckingTransactions_ShouldCarryNoInvoiceData()
    {
        var consumer = new TransactionsBatchIngestedConsumer(
            _handler,
            _matchingEngine,
            NullLogger<TransactionsBatchIngestedConsumer>.Instance);

        var checkingTx = new TransactionIngested(
            IngestionId: Guid.NewGuid(),
            UserId: "user-01",
            Source: "Banco Inter",
            AccountId: "acc-inter-checking",
            BankTransactionId: "tx-1",
            Amount: 150.00m,
            TransactionDate: DateTime.UtcNow,
            Description: "PIX Recebido",
            Currency: "BRL",
            RawPayloadJson: "{}",
            OccurredAtUtc: DateTime.UtcNow);

        var batch = new TransactionsBatchIngested(
            BatchId: Guid.NewGuid(),
            UserId: "user-01",
            ChunkIndex: 0,
            TotalChunks: 1,
            CheckingTransactions: new[] { checkingTx },
            CardTransactions: Array.Empty<InvoiceItemIngested>(),
            OccurredAtUtc: DateTime.UtcNow);

        var context = Substitute.For<ConsumeContext<TransactionsBatchIngested>>();
        context.Message.Returns(batch);
        context.CancellationToken.Returns(CancellationToken.None);

        await consumer.Consume(context);

        await _handler.Received(1).Handle(
            Arg.Is<IngestTransactionCommand>(c => c.InvoiceDueDateUtc == null),
            Arg.Any<CancellationToken>());
    }

    // ─── Helpers ───────────────────────────────────────────────────────────────

    private static InvoiceItemIngested BuildInvoiceItem(
        DateTime? dueDate = null,
        int? currentInstallment = 1,
        int? totalInstallments = 1,
        bool omitDueDate = false)
        => new(
            IngestionId: Guid.NewGuid(),
            UserId: "user-01",
            Source: "Itau",
            CreditCardAccountId: "acc-itau-card",
            CardLastFourDigits: "1234",
            BankTransactionId: "tx-card-1",
            Amount: 250.00m,
            TransactionDate: new DateTime(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc),
            Description: "Supermercado",
            Category: "Alimentação",
            CurrentInstallment: currentInstallment,
            TotalInstallments: totalInstallments,
            InvoiceDueDate: omitDueDate ? null : dueDate ?? DueDate,
            Currency: "BRL",
            RawPayloadJson: "{}",
            OccurredAtUtc: DateTime.UtcNow);

    private static ConsumeContext<InvoiceItemIngested> BuildContext(InvoiceItemIngested message)
    {
        var context = Substitute.For<ConsumeContext<InvoiceItemIngested>>();
        context.Message.Returns(message);
        context.CancellationToken.Returns(CancellationToken.None);
        return context;
    }
}
