using FinanceHub.AuthConsent.Application.Commands.AuthorizeConsent;
using FinanceHub.AuthConsent.Application.DTOs;
using FinanceHub.AuthConsent.Application.Exceptions;
using FinanceHub.AuthConsent.Application.Interfaces;
using FinanceHub.AuthConsent.Domain.Constants;
using FinanceHub.AuthConsent.Domain.Entities;
using FinanceHub.Shared.Messaging.Events;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Xunit;

namespace FinanceHub.UnitTests.AuthConsent.Application;

public class AuthorizeConsentCommandHandlerTests
{
    private readonly IBankConsentRepository _repository = Substitute.For<IBankConsentRepository>();
    private readonly IKeyedOAuthStrategyFactory _strategyFactory = Substitute.For<IKeyedOAuthStrategyFactory>();
    private readonly IOAuthBankClientStrategy _oauthStrategy = Substitute.For<IOAuthBankClientStrategy>();
    private readonly IEventPublisher _eventPublisher = Substitute.For<IEventPublisher>();
    private readonly FakeTimeProvider _fakeTimeProvider = new();

    public AuthorizeConsentCommandHandlerTests()
    {
        _fakeTimeProvider.SetUtcNow(new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero));
        _strategyFactory.GetStrategy(BankIdentifiers.Itau).Returns(_oauthStrategy);
    }

    [Fact]
    public async Task Handle_ComCodigoValido_DeveChamarStrategy_PersistirAgregado_E_PublicarOutbox()
    {
        var consent = BankConsent.Request("user-123", BankIdentifiers.Itau, "external-consent-999", _fakeTimeProvider);
        _repository.GetByIdAsync(consent.Id, Arg.Any<CancellationToken>())
                   .Returns(consent);

        _oauthStrategy.ExchangeCodeForTokensAsync("auth-code-007", "redirect-uri", Arg.Any<CancellationToken>())
                      .Returns(new OAuthTokenExchangeResult("acc-token-123", "ref-token-456", 3600));

        var handler = new AuthorizeConsentCommandHandler(_repository, _strategyFactory, _eventPublisher, _fakeTimeProvider);
        var command = new AuthorizeConsentCommand(consent.Id, "auth-code-007", "redirect-uri");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.ConsentId.Should().Be(consent.Id);
        result.Status.Should().Be(ConsentStatus.Authorized.ToString());

        await _repository.Received(1).UpdateAsync(Arg.Is<BankConsent>(c => c.Status == ConsentStatus.Authorized), Arg.Any<CancellationToken>());
        await _eventPublisher.Received(1).PublishAsync(Arg.Is<BankAccountLinked>(e => e.UserId == "user-123" && e.InstitutionId == BankIdentifiers.Itau), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ComConsentimentoInexistente_DeveLancarConsentNotFoundDomainException()
    {
        var consentId = Guid.NewGuid();
        _repository.GetByIdAsync(consentId, Arg.Any<CancellationToken>())
                   .Returns((BankConsent?)null);

        var handler = new AuthorizeConsentCommandHandler(_repository, _strategyFactory, _eventPublisher, _fakeTimeProvider);
        var command = new AuthorizeConsentCommand(consentId, "auth-code-007", "redirect-uri");

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConsentNotFoundDomainException>()
                 .WithMessage($"Consentimento '{consentId}' não foi localizado no repositório.");
    }
}
