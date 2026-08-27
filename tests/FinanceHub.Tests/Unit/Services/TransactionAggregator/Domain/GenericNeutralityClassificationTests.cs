using System;
using FinanceHub.TransactionAggregator.Domain.Constants;
using FinanceHub.TransactionAggregator.Domain.Entities;
using FinanceHub.TransactionAggregator.Domain.Services;
using FinanceHub.TransactionAggregator.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace FinanceHub.Tests.Services.TransactionAggregator.Domain;

/// <summary>
/// A classificação de neutralidade era feita casando texto da descrição contra valores de um
/// usuário específico — incluindo o nome completo dele — dentro do handler de ingestão. Estes
/// testes travam a versão genérica: a neutralidade deriva da natureza econômica da categoria,
/// e nada aqui conhece nenhum titular.
/// </summary>
public class GenericNeutralityClassificationTests
{
    [Theory]
    [InlineData(TransactionNature.Transfer)]
    [InlineData(TransactionNature.Investment)]
    [InlineData(TransactionNature.BillPayment)]
    [InlineData(TransactionNature.Adjustment)]
    public void ApplyNature_WhenMoneyOnlyChangesPlace_ShouldBeNeutralInTotals(TransactionNature nature)
    {
        var transaction = BuildTransaction();

        transaction.ApplyNature(nature);

        transaction.Nature.Should().Be(nature);
        transaction.IsIgnoredInTotals.Should().BeTrue();
    }

    [Fact]
    public void ApplyNature_WhenOperating_ShouldCountInTotals()
    {
        var transaction = BuildTransaction();

        transaction.ApplyNature(TransactionNature.Operating);

        transaction.Nature.Should().Be(TransactionNature.Operating);
        transaction.IsIgnoredInTotals.Should().BeFalse();
    }

    [Fact]
    public void ApplyNature_ShouldNotDependOnDescriptionContent()
    {
        // Duas descrições completamente diferentes, mesma natureza: mesmo resultado.
        // É a garantia de que não sobrou casamento de texto na decisão.
        var a = BuildTransaction("PIX ENVIADO FULANO DE TAL");
        var b = BuildTransaction("QUALQUER COISA ALEATORIA 123");

        a.ApplyNature(TransactionNature.Transfer);
        b.ApplyNature(TransactionNature.Transfer);

        a.IsIgnoredInTotals.Should().Be(b.IsIgnoredInTotals);
    }

    // ─── Detector de pagamento de fatura (vocabulário de mercado) ──────────────

    [Theory]
    [InlineData("PAGAMENTO DE FATURA CARTAO")]
    [InlineData("Pagto Fatura Itau")]
    [InlineData("PAGAMENTO CARTÃO DE CRÉDITO")]
    public void IsBillPayment_WhenBrazilianBankVocabulary_ShouldDetect(string description)
    {
        TransactionBillPaymentDetector.IsBillPayment(description).Should().BeTrue();
    }

    [Theory]
    [InlineData("SUPERMERCADO CARREFOUR")]
    [InlineData("TARIFA MENSALIDADE PACOTE SERVICOS")]
    [InlineData("FATURAMENTO CONSULTORIA")]
    [InlineData(null)]
    [InlineData("")]
    public void IsBillPayment_WhenNotABillPayment_ShouldNotDetect(string? description)
    {
        TransactionBillPaymentDetector.IsBillPayment(description).Should().BeFalse();
    }

    [Fact]
    public void IsBillPayment_ShouldNotMatchBankFees()
    {
        // Regressão: a versão anterior marcava a categoria Finanças > Tarifas como pagamento de
        // fatura, fazendo toda tarifa bancária sumir dos totais.
        TransactionBillPaymentDetector.IsBillPayment("TARIFA PACOTE DE SERVICOS").Should().BeFalse();
        SystemCategoryIds.Tarifas.Should().NotBe(SystemCategoryIds.Investimentos);
    }

    // ─── Catálogo de categorias ────────────────────────────────────────────────

    [Fact]
    public void SystemCategoryIds_ShouldMatchSeededCatalog()
    {
        SystemCategoryIds.Transferencias.Should().Be(Guid.Parse("11111111-1111-1111-1111-111111111002"));
        SystemCategoryIds.Investimentos.Should().Be(Guid.Parse("11111111-1111-1111-1111-111111110805"));
        SystemCategoryIds.Ajustes.Should().Be(Guid.Parse("11111111-1111-1111-1111-111111111001"));
    }

    [Fact]
    public void Category_WhenNatureNotDeclared_ShouldDefaultToOperating()
    {
        var category = Category.Create("Mercado", "market", "cart", "emerald");

        category.Nature.Should().Be(TransactionNature.Operating);
    }

    [Fact]
    public void Category_ShouldCarryDeclaredNature()
    {
        var category = Category.Create(
            "Transferências", "transfers", "arrow", "gray",
            nature: TransactionNature.Transfer);

        category.Nature.Should().Be(TransactionNature.Transfer);
    }

    private static CanonicalTransaction BuildTransaction(string description = "COMPRA QUALQUER")
        => CanonicalTransaction.Create(new CanonicalTransactionCreationParams(
            "user-1",
            new AccountIdentifier("itau", "acc-1"),
            new TransactionHash(new string('a', 64)),
            new Money(100m, "BRL"),
            TransactionType.Debit,
            SanitizedDescription.Create(description),
            Guid.NewGuid(),
            CategorizationSource.GlobalRule,
            DateTime.UtcNow,
            new BankTransactionDetails("tx-1", TransactionChannel.Pix, "Merchant")));
}
