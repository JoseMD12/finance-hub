using System;
using FinanceHub.Shared.Messaging.Events;
using FinanceHub.TransactionAggregator.Domain.Entities;
using FinanceHub.TransactionAggregator.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace FinanceHub.Tests.Services.TransactionAggregator.Infrastructure;

/// <summary>
/// Num deploy parcial, o consumer novo processa mensagens publicadas pela versão anterior — ou
/// que já estavam na fila quando o deploy começou. Nessas mensagens os campos de crédito chegam
/// com o valor padrão.
///
/// Enquanto <c>IsCreditCard</c> era <c>bool</c>, "não informado" era indistinguível de "não é
/// cartão", e cada mensagem antiga apagava limite, limite disponível e datas de fatura das contas
/// já sincronizadas. Como o snapshot só é republicado no próximo sync, o painel ficaria sem dados
/// de cartão até lá. Estes testes travam essa regressão.
/// </summary>
public class CreditDataPartialDeploySafetyTests
{
    private const string UserId = "user-1";

    [Fact]
    public void SnapshotItem_FromOlderPublisher_ShouldReportUnknownRatherThanNotACreditCard()
    {
        // Construtor com apenas os campos que a versão anterior conhecia.
        var item = new AccountBalanceSnapshotItem(
            AccountId: "acc-1",
            InstitutionId: "itau",
            AccountType: "CREDIT",
            CurrentBalance: -2765.27m,
            Currency: "BRL",
            SnapshotAtUtc: DateTime.UtcNow);

        item.IsCreditCard.Should().BeNull("ausência de informação não pode ser lida como negativa");
        item.CreditLimit.Should().BeNull();
        item.InvoiceDueDateUtc.Should().BeNull();
    }

    [Fact]
    public void SnapshotItem_FromCurrentPublisher_ShouldAssertCreditCardExplicitly()
    {
        var item = BuildCurrentItem(isCreditCard: true);

        item.IsCreditCard.Should().BeTrue();
        item.CreditLimit.Should().Be(12150m);
    }

    [Fact]
    public void SnapshotItem_WhenPublisherAssertsNotACard_ShouldBeFalseNotNull()
    {
        var item = BuildCurrentItem(isCreditCard: false);

        // Afirmar "não é cartão" continua possível e é diferente de omitir.
        item.IsCreditCard.Should().BeFalse();
    }

    [Fact]
    public void SynchronizeCreditData_ShouldOverwriteWhenNewInformationArrives()
    {
        var balance = BuildBalanceWithCreditData();

        balance.SynchronizeCreditData(new CreditAccountInfo(
            isCreditCard: true,
            creditLimit: 20_000m,
            availableCreditLimit: 15_000m));

        balance.CreditInfo.CreditLimit.Should().Be(20_000m);
        balance.CreditInfo.UsedCreditLimit.Should().Be(5_000m);
    }

    [Fact]
    public void AccountBalance_WhenNotSynchronized_ShouldKeepPreviousCreditData()
    {
        // Espelha o caminho do consumer quando a mensagem não traz informação de crédito:
        // SynchronizeCreditData simplesmente não é chamado, e o estado anterior sobrevive.
        var balance = BuildBalanceWithCreditData();

        balance.SynchronizeWithBankSnapshot(new Money(-3000m, "BRL"), DateTime.UtcNow);

        balance.CreditInfo.IsCreditCard.Should().BeTrue();
        balance.CreditInfo.CreditLimit.Should().Be(12_150m);
        balance.CreditInfo.InvoiceDueDateUtc.Should().NotBeNull();
    }

    private static AccountBalanceSnapshotItem BuildCurrentItem(bool isCreditCard)
        => new(
            AccountId: "acc-1",
            InstitutionId: "itau",
            AccountType: isCreditCard ? "CREDIT" : "BANK",
            CurrentBalance: -2765.27m,
            Currency: "BRL",
            SnapshotAtUtc: DateTime.UtcNow,
            IsCreditCard: isCreditCard,
            CreditLimit: isCreditCard ? 12150m : null,
            AvailableCreditLimit: isCreditCard ? 9384.73m : null,
            InvoiceDueDateUtc: isCreditCard ? new DateTime(2026, 8, 25, 0, 0, 0, DateTimeKind.Utc) : null,
            InvoiceClosingDateUtc: null);

    private static AccountBalance BuildBalanceWithCreditData()
    {
        var balance = AccountBalance.Create(
            UserId,
            new AccountIdentifier("itau", "acc-1"),
            new Money(-2765.27m, "BRL"));

        balance.SynchronizeCreditData(new CreditAccountInfo(
            isCreditCard: true,
            creditLimit: 12_150m,
            availableCreditLimit: 9_384.73m,
            invoiceDueDateUtc: new DateTime(2026, 8, 25, 0, 0, 0, DateTimeKind.Utc)));

        return balance;
    }
}
