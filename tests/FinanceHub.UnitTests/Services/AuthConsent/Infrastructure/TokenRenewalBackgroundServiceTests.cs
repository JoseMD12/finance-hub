using FinanceHub.AuthConsent.Application.DTOs;
using FinanceHub.AuthConsent.Application.Interfaces;
using FinanceHub.AuthConsent.Domain.Constants;
using FinanceHub.AuthConsent.Domain.Entities;
using FinanceHub.AuthConsent.Infrastructure.BackgroundServices;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Xunit;

namespace FinanceHub.UnitTests.AuthConsent.Infrastructure;

public class TokenRenewalBackgroundServiceTests
{
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly IServiceScope _scope = Substitute.For<IServiceScope>();
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();
    private readonly IBankConsentRepository _repository = Substitute.For<IBankConsentRepository>();
    private readonly IKeyedOAuthStrategyFactory _strategyFactory = Substitute.For<IKeyedOAuthStrategyFactory>();
    private readonly IOAuthBankClientStrategy _itauOAuthStrategy = Substitute.For<IOAuthBankClientStrategy>();
    private readonly IOAuthBankClientStrategy _interOAuthStrategy = Substitute.For<IOAuthBankClientStrategy>();
    private readonly ILogger<TokenRenewalBackgroundService> _logger = Substitute.For<ILogger<TokenRenewalBackgroundService>>();
    private readonly FakeTimeProvider _fakeTimeProvider = new();

    public TokenRenewalBackgroundServiceTests()
    {
        _fakeTimeProvider.SetUtcNow(new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero));

        _scopeFactory.CreateScope().Returns(_scope);
        _scope.ServiceProvider.Returns(_serviceProvider);
        _serviceProvider.GetService(typeof(IBankConsentRepository)).Returns(_repository);
        _serviceProvider.GetService(typeof(IKeyedOAuthStrategyFactory)).Returns(_strategyFactory);
        _strategyFactory.GetStrategy(BankIdentifiers.Itau).Returns(_itauOAuthStrategy);
        _strategyFactory.GetStrategy(BankIdentifiers.Inter).Returns(_interOAuthStrategy);
    }

    [Fact]
    public async Task ProcessTokenRenewalAsync_QuandoHouverConsentimentoItauExpiringSoon_DeveRotacionarTokensEPersistir()
    {
        var consent = BankConsent.Request("user-123", BankIdentifiers.Itau, "consent-999", _fakeTimeProvider);
        consent.Authorize("acc-old", "ref-old", 3600, _fakeTimeProvider);

        _fakeTimeProvider.Advance(TimeSpan.FromMinutes(56));

        _repository.GetExpiringConsentsAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
                   .Returns([consent]);

        _itauOAuthStrategy.RefreshTokenAsync("ref-old", Arg.Any<CancellationToken>())
                      .Returns(new OAuthTokenExchangeResult("acc-new-999", "ref-new-888", 3600));

        var worker = new TokenRenewalBackgroundService(_scopeFactory, _logger, _fakeTimeProvider);

        await worker.ProcessTokenRenewalAsync(CancellationToken.None);

        await _repository.Received(1).UpdateAsync(Arg.Is<BankConsent>(c => c.Token.AccessToken == "acc-new-999" && c.Token.RefreshToken == "ref-new-888"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessTokenRenewalAsync_QuandoHouverConsentimentoBancoInterExpiringSoon_DeveRotacionarTokensEPersistir()
    {
        var consent = BankConsent.Request("user-456", BankIdentifiers.Inter, "inter-consent-777", _fakeTimeProvider);
        consent.Authorize("inter-acc-old", "inter-ref-old", 3600, _fakeTimeProvider);

        _fakeTimeProvider.Advance(TimeSpan.FromMinutes(56));

        _repository.GetExpiringConsentsAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
                   .Returns([consent]);

        _interOAuthStrategy.RefreshTokenAsync("inter-ref-old", Arg.Any<CancellationToken>())
                       .Returns(new OAuthTokenExchangeResult("inter-acc-new-111", "inter-ref-new-222", 3600));

        var worker = new TokenRenewalBackgroundService(_scopeFactory, _logger, _fakeTimeProvider);

        await worker.ProcessTokenRenewalAsync(CancellationToken.None);

        await _repository.Received(1).UpdateAsync(Arg.Is<BankConsent>(c => c.Token.AccessToken == "inter-acc-new-111" && c.Token.RefreshToken == "inter-ref-new-222"), Arg.Any<CancellationToken>());
    }
}
