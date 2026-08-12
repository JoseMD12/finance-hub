using FinanceHub.AuthConsent.Application.Commands.RenewToken;
using FinanceHub.AuthConsent.Application.DTOs;
using FinanceHub.AuthConsent.Application.Interfaces;
using FinanceHub.AuthConsent.Domain.Constants;
using FinanceHub.AuthConsent.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Xunit;

namespace FinanceHub.UnitTests.AuthConsent.Application;

public class RenewTokenCommandHandlerTests
{
    private readonly IBankConsentRepository _repository = Substitute.For<IBankConsentRepository>();
    private readonly IKeyedOAuthStrategyFactory _strategyFactory = Substitute.For<IKeyedOAuthStrategyFactory>();
    private readonly IOAuthBankClientStrategy _oauthStrategy = Substitute.For<IOAuthBankClientStrategy>();
    private readonly FakeTimeProvider _timeProvider = new();

    [Fact]
    public async Task Handle_ComConsentimentoAutorizado_DeveRotacionarTokensEAtualizarRepositorio()
    {
        _timeProvider.SetUtcNow(new DateTimeOffset(2026, 8, 12, 10, 0, 0, TimeSpan.Zero));

        var consent = BankConsent.Request("user-123", BankIdentifiers.MercadoPago, "ext-mp-1", _timeProvider);
        consent.Authorize("old-access", "old-refresh", 3600, _timeProvider);

        _repository.GetByIdAsync(consent.Id, Arg.Any<CancellationToken>()).Returns(consent);
        _strategyFactory.GetStrategy(BankIdentifiers.MercadoPago).Returns(_oauthStrategy);
        _oauthStrategy.RefreshTokenAsync("old-refresh", Arg.Any<CancellationToken>())
                      .Returns(new OAuthTokenExchangeResult("new-access", "new-refresh", 3600));

        var handler = new RenewTokenCommandHandler(_repository, _strategyFactory, _timeProvider);
        var command = new RenewTokenCommand(consent.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.AccessToken.Should().Be("new-access");
        result.RefreshToken.Should().Be("new-refresh");

        consent.Token.AccessToken.Should().Be("new-access");
        consent.Token.RefreshToken.Should().Be("new-refresh");

        await _repository.Received(1).UpdateAsync(consent, Arg.Any<CancellationToken>());
    }
}
