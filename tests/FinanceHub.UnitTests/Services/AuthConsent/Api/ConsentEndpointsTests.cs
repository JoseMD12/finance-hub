using System.Net;
using System.Net.Http.Json;
using FinanceHub.AuthConsent.Api;
using FinanceHub.AuthConsent.Api.Endpoints;
using FinanceHub.AuthConsent.Application.DTOs;
using FinanceHub.AuthConsent.Application.Interfaces;
using FinanceHub.AuthConsent.Domain.Constants;
using FinanceHub.AuthConsent.Domain.Entities;
using FinanceHub.Shared.Messaging.Events;
using FinanceHub.UnitTests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Xunit;

namespace FinanceHub.UnitTests.AuthConsent.Api;

public class ConsentEndpointsTests
{
    [Fact]
    public async Task GetHealth_DeveRetornarStatus200OK_E_Healthy()
    {
        using var factory = new CustomWebApplicationFactory<Program>();
        await factory.InitializeAsync();
        var client = factory.CreateClient();

        try
        {
            var response = await client.GetAsync("/health");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await factory.DisposeAsync();
        }
    }

    [Fact]
    public async Task AuthorizeConsent_ComDadosValidos_DeveRetornar200OK_E_ConsentResponse()
    {
        var repository = Substitute.For<IBankConsentRepository>();
        var strategyFactory = Substitute.For<IKeyedOAuthStrategyFactory>();
        var oauthStrategy = Substitute.For<IOAuthBankClientStrategy>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var fakeTimeProvider = new FakeTimeProvider();

        fakeTimeProvider.SetUtcNow(new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero));
        var consent = BankConsent.Request("user-123", BankIdentifiers.Itau, "ext-consent-999", fakeTimeProvider);

        repository.GetByIdAsync(consent.Id, Arg.Any<CancellationToken>()).Returns(consent);
        strategyFactory.GetStrategy(BankIdentifiers.Itau).Returns(oauthStrategy);
        oauthStrategy.ExchangeCodeForTokensAsync("auth-code-123", "https://redirect.uri", Arg.Any<CancellationToken>())
                      .Returns(new OAuthTokenExchangeResult("access-token-1", "refresh-token-1", 3600));

        using var factory = new CustomWebApplicationFactory<Program>();
        await factory.InitializeAsync();

        var client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped(_ => repository);
                services.AddScoped(_ => strategyFactory);
                services.AddScoped(_ => eventPublisher);
                services.AddSingleton<TimeProvider>(fakeTimeProvider);
            });
        }).CreateClient();

        try
        {
            var requestPayload = new AuthorizeConsentRequest("auth-code-123", "https://redirect.uri");
            var response = await client.PostAsJsonAsync($"/api/v1/consents/{consent.Id}/authorize", requestPayload);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ConsentResponseDto>();

            result.Should().NotBeNull();
            result!.ConsentId.Should().Be(consent.Id);
            result.Status.Should().Be(ConsentStatus.Authorized.ToString());
        }
        finally
        {
            await factory.DisposeAsync();
        }
    }
}
