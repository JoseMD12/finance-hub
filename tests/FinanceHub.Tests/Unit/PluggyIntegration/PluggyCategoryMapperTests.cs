using FinanceHub.PluggyIntegration.Domain.Constants;
using FluentAssertions;
using Xunit;

namespace FinanceHub.Tests.PluggyIntegration;

public class PluggyCategoryMapperTests
{
    [Theory]
    [InlineData("Transfer - PIX", "Transferência")]
    [InlineData("Proceeds interests and dividends", "Rendimentos")]
    [InlineData("Eating out", "Alimentação")]
    [InlineData("Groceries", "Supermercado")]
    [InlineData("Parking", "Transporte")]
    [InlineData("Clothing", "Vestuário")]
    [InlineData("Digital services", "Serviços Digitais")]
    [InlineData("Credit card payment", "Pagamento de Fatura")]
    [InlineData("Services", "Serviços")]
    [InlineData("Unknown Nonexistent Category", "Outros")]
    [InlineData(null, "Outros")]
    [InlineData("", "Outros")]
    public void MapCategory_ShouldReturnCorrectCanonicalCategory(string? rawCategory, string expectedCategory)
    {
        // Act
        var result = PluggyCategoryMapper.Map(rawCategory);

        // Assert
        result.Should().Be(expectedCategory);
    }
}
