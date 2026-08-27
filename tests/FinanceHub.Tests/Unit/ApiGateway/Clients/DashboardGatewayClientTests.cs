using System.Net;
using System.Net.Http.Json;

using FinanceHub.ApiGateway.Clients;
using FinanceHub.ApiGateway.DTOs;
using FinanceHub.Tests.Helpers;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Xunit;

namespace FinanceHub.Tests.ApiGateway.Clients;

/// <summary>
/// O Gateway é um repassador: o valor dele no Dashboard está em encaminhar o filtro de período
/// corretamente e em não derrubar a tela quando o serviço downstream não responde.
/// </summary>
public class DashboardGatewayClientTests
{
    private const string UserId = "user-123";

    private readonly ILogger<TransactionAggregatorServiceClient> _logger =
        Substitute.For<ILogger<TransactionAggregatorServiceClient>>();

    [Fact]
    public async Task GetDashboardSummaryAsync_ShouldForwardEveryFilterAsQueryString()
    {
        var handler = BuildHandler(BuildSummary());
        var client = BuildClient(handler);

        await client.GetDashboardSummaryAsync(new GatewayDashboardFilterDto(
            UserId: UserId,
            StartDate: new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate: new DateTime(2026, 8, 31, 0, 0, 0, DateTimeKind.Utc),
            InstitutionId: "itau",
            IncludeIgnoredInTotals: true));

        var url = handler.LastRequest!.RequestUri!.ToString();
        url.Should().Contain("/api/v1/dashboard/summary");
        url.Should().Contain($"userId={UserId}");
        url.Should().Contain("startDate=2026-08-01");
        url.Should().Contain("endDate=2026-08-31");
        url.Should().Contain("institutionId=itau");
        url.Should().Contain("includeIgnoredInTotals=true");
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_WhenNoOptionalFilters_ShouldSendOnlyUserId()
    {
        var handler = BuildHandler(BuildSummary());
        var client = BuildClient(handler);

        await client.GetDashboardSummaryAsync(new GatewayDashboardFilterDto(UserId));

        var url = handler.LastRequest!.RequestUri!.ToString();
        url.Should().Contain($"userId={UserId}");
        url.Should().NotContain("startDate");
        url.Should().NotContain("institutionId");
        // Flag falsa não vira parâmetro: mantém a URL limpa e o cache do downstream previsível.
        url.Should().NotContain("includeIgnoredInTotals");
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_WhenSuccess_ShouldReturnAggregatedSummary()
    {
        var handler = BuildHandler(BuildSummary());
        var client = BuildClient(handler);

        var result = await client.GetDashboardSummaryAsync(new GatewayDashboardFilterDto(UserId));

        result.UserId.Should().Be(UserId);
        result.Summary.TotalIncome.Should().Be(8500m);
        result.CategoryExpenses.Should().ContainSingle();
        result.InstitutionBalances.Should().ContainSingle();
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_WhenDownstreamReturnsEmptyBody_ShouldDegradeToEmptyDashboard()
    {
        // Sem isto, um corpo vazio viraria NullReferenceException e derrubaria a tela inteira em
        // vez de mostrar o estado sem dados.
        var handler = new MockHttpMessageHandler
        {
            ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("null", System.Text.Encoding.UTF8, "application/json")
            }
        };
        var client = BuildClient(handler);

        var result = await client.GetDashboardSummaryAsync(new GatewayDashboardFilterDto(UserId));

        result.UserId.Should().Be(UserId);
        result.Summary.TotalIncome.Should().Be(0m);
        result.CategoryExpenses.Should().BeEmpty();
        result.RecentTransactions.Should().BeEmpty();
        result.MonthlyCashFlow.Should().BeEmpty();
        result.InstitutionBalances.Should().BeEmpty();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static MockHttpMessageHandler BuildHandler(GatewayDashboardSummaryDto payload)
        => new()
        {
            ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(payload)
            }
        };

    private static TransactionAggregatorServiceClient BuildClient(MockHttpMessageHandler handler)
        => new(
            new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5002") },
            Substitute.For<ILogger<TransactionAggregatorServiceClient>>());

    private static GatewayDashboardSummaryDto BuildSummary()
        => new(
            UserId: UserId,
            Summary: new GatewayTransactionSummaryDto(8500m, 5400m, 3100m, 42),
            InstitutionBalances:
            [
                new GatewayInstitutionBalanceDto(
                    "itau", "acc-1", 1650.59m, "BRL",
                    IsCreditCard: false,
                    CreditLimit: null,
                    AvailableCreditLimit: null,
                    UsedCreditLimit: null,
                    InvoiceDueDateUtc: null,
                    LastUpdatedAtUtc: DateTime.UtcNow)
            ],
            CategoryExpenses:
            [
                new GatewayCategoryExpenseDto(Guid.NewGuid(), "Alimentação", "emerald", "utensils", 1200m, 100m)
            ],
            MonthlyCashFlow: [new GatewayMonthlyCashFlowPointDto(2026, 8, 8500m, 5400m, 3100m)],
            RecentTransactions: [],
            GeneratedAtUtc: DateTime.UtcNow);
}
