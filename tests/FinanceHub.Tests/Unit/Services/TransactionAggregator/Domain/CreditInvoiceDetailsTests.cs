using System;
using FinanceHub.TransactionAggregator.Domain.Entities;
using FinanceHub.TransactionAggregator.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace FinanceHub.Tests.Services.TransactionAggregator.Domain;

/// <summary>
/// Cobre os dados de fatura carregados por transação de cartão de crédito.
/// Antes desta funcionalidade, o vencimento da fatura chegava no evento
/// <c>InvoiceItemIngested</c> e era silenciosamente descartado pelos consumers.
/// </summary>
public class CreditInvoiceDetailsTests
{
    private const string BankTransactionId = "tx-card-001";
    private const string MerchantName = "Supermercado";

    // ─── BankTransactionDetails ────────────────────────────────────────────────

    [Fact]
    public void Constructor_WhenNoCreditDataProvided_ShouldDefaultToEmptyInvoiceDetails()
    {
        var details = new BankTransactionDetails(BankTransactionId, TransactionChannel.Pix, MerchantName);

        details.InvoiceDueDateUtc.Should().BeNull();
        details.CurrentInstallment.Should().BeNull();
        details.TotalInstallments.Should().BeNull();
        details.IsInstallment.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WhenInvoiceDataProvided_ShouldPreserveDueDateAndInstallments()
    {
        var dueDate = new DateTime(2026, 9, 5, 0, 0, 0, DateTimeKind.Utc);

        var details = new BankTransactionDetails(
            BankTransactionId,
            TransactionChannel.CreditCard,
            MerchantName,
            invoiceDueDateUtc: dueDate,
            currentInstallment: 2,
            totalInstallments: 6);

        details.InvoiceDueDateUtc.Should().Be(dueDate);
        details.CurrentInstallment.Should().Be(2);
        details.TotalInstallments.Should().Be(6);
        details.IsInstallment.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WhenDueDateHasUnspecifiedKind_ShouldNormalizeToUtc()
    {
        var unspecified = new DateTime(2026, 9, 5, 0, 0, 0, DateTimeKind.Unspecified);

        var details = new BankTransactionDetails(
            BankTransactionId,
            TransactionChannel.CreditCard,
            MerchantName,
            invoiceDueDateUtc: unspecified);

        details.InvoiceDueDateUtc!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Constructor_WhenSinglePaymentPurchase_ShouldNotBeConsideredInstallment()
    {
        var details = new BankTransactionDetails(
            BankTransactionId,
            TransactionChannel.CreditCard,
            MerchantName,
            currentInstallment: 1,
            totalInstallments: 1);

        details.IsInstallment.Should().BeFalse();
    }

    [Theory]
    [InlineData(0, 6)]
    [InlineData(-1, 6)]
    [InlineData(1, 0)]
    [InlineData(1, -3)]
    public void Constructor_WhenInstallmentNumbersAreNotPositive_ShouldThrow(int current, int total)
    {
        var act = () => new BankTransactionDetails(
            BankTransactionId,
            TransactionChannel.CreditCard,
            MerchantName,
            currentInstallment: current,
            totalInstallments: total);

        act.Should().Throw<InvalidInstallmentDomainException>();
    }

    [Fact]
    public void Constructor_WhenCurrentInstallmentExceedsTotal_ShouldThrow()
    {
        var act = () => new BankTransactionDetails(
            BankTransactionId,
            TransactionChannel.CreditCard,
            MerchantName,
            currentInstallment: 7,
            totalInstallments: 6);

        act.Should().Throw<InvalidInstallmentDomainException>();
    }

    [Fact]
    public void Constructor_WhenOnlyOneInstallmentFieldIsKnown_ShouldAcceptIt()
    {
        // Connectors da Pluggy podem devolver apenas o total, sem a posição atual.
        var details = new BankTransactionDetails(
            BankTransactionId,
            TransactionChannel.CreditCard,
            MerchantName,
            totalInstallments: 10);

        details.CurrentInstallment.Should().BeNull();
        details.TotalInstallments.Should().Be(10);
        details.IsInstallment.Should().BeTrue();
    }

    // ─── AccountBalance · dados de crédito da conta ────────────────────────────

    [Fact]
    public void AccountBalance_WhenCreated_ShouldStartWithoutCreditData()
    {
        var balance = BuildBalance();

        balance.CreditInfo.IsCreditCard.Should().BeFalse();
        balance.CreditInfo.CreditLimit.Should().BeNull();
        balance.CreditInfo.AvailableCreditLimit.Should().BeNull();
        balance.CreditInfo.InvoiceDueDateUtc.Should().BeNull();
    }

    [Fact]
    public void SynchronizeCreditData_WhenCardSnapshotArrives_ShouldStoreLimitsAndDueDate()
    {
        var balance = BuildBalance();
        var dueDate = new DateTime(2026, 9, 5, 0, 0, 0, DateTimeKind.Utc);
        var closingDate = new DateTime(2026, 8, 25, 0, 0, 0, DateTimeKind.Utc);

        balance.SynchronizeCreditData(new CreditAccountInfo(
            isCreditCard: true,
            creditLimit: 10_000m,
            availableCreditLimit: 6_500m,
            invoiceDueDateUtc: dueDate,
            invoiceClosingDateUtc: closingDate));

        balance.CreditInfo.IsCreditCard.Should().BeTrue();
        balance.CreditInfo.CreditLimit.Should().Be(10_000m);
        balance.CreditInfo.AvailableCreditLimit.Should().Be(6_500m);
        balance.CreditInfo.InvoiceDueDateUtc.Should().Be(dueDate);
        balance.CreditInfo.InvoiceClosingDateUtc.Should().Be(closingDate);
    }

    [Fact]
    public void UsedCreditLimit_WhenBothLimitsKnown_ShouldReturnTheDifference()
    {
        var info = new CreditAccountInfo(
            isCreditCard: true,
            creditLimit: 10_000m,
            availableCreditLimit: 6_500m);

        info.UsedCreditLimit.Should().Be(3_500m);
    }

    [Fact]
    public void UsedCreditLimit_WhenAvailableExceedsLimit_ShouldNotReturnNegative()
    {
        // Alguns connectors devolvem limite disponível maior que o limite total
        // logo após um pagamento de fatura ainda não compensado.
        var info = new CreditAccountInfo(
            isCreditCard: true,
            creditLimit: 10_000m,
            availableCreditLimit: 10_500m);

        info.UsedCreditLimit.Should().Be(0m);
    }

    [Fact]
    public void UsedCreditLimit_WhenLimitsAreUnknown_ShouldReturnNull()
    {
        var info = new CreditAccountInfo(isCreditCard: true);

        info.UsedCreditLimit.Should().BeNull();
    }

    [Fact]
    public void SynchronizeCreditData_WhenCalledWithNull_ShouldThrow()
    {
        var balance = BuildBalance();

        var act = () => balance.SynchronizeCreditData(null!);

        act.Should().Throw<TransactionAggregatorDomainException>();
    }

    private static AccountBalance BuildBalance() => AccountBalance.Create(
        "user-1",
        new FinanceHub.TransactionAggregator.Domain.ValueObjects.AccountIdentifier("itau", "acc-100"),
        new FinanceHub.TransactionAggregator.Domain.ValueObjects.Money(0m, "BRL"));
}
