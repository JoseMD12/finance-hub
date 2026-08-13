using FinanceHub.AuthConsent.Application.Commands.CreateConsent;
using FinanceHub.AuthConsent.Application.Interfaces;
using FinanceHub.AuthConsent.Domain.Constants;
using FinanceHub.AuthConsent.Domain.Entities;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FinanceHub.UnitTests.AuthConsent.Application;

public class CreateConsentCommandHandlerTests
{
    private readonly IBankConsentRepository _repository = Substitute.For<IBankConsentRepository>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();

    [Fact]
    public async Task Handle_ComDadosValidos_DeveCriarEObterIdDoConsentimentoPendente()
    {
        var fixedTime = new DateTimeOffset(2026, 8, 12, 10, 0, 0, TimeSpan.Zero);
        _timeProvider.GetUtcNow().Returns(fixedTime);

        var handler = new CreateConsentCommandHandler(_repository, _timeProvider);
        var command = new CreateConsentCommand("user-123", BankIdentifiers.Itau, "ext-consent-001");

        var consentId = await handler.Handle(command, CancellationToken.None);

        consentId.Should().NotBeEmpty();
        await _repository.Received(1).AddAsync(Arg.Is<BankConsent>(c =>
            c.UserId == "user-123" &&
            c.InstitutionId == BankIdentifiers.Itau &&
            c.Token.ExternalConsentId == "ext-consent-001" &&
            c.Status == ConsentStatus.Pending), Arg.Any<CancellationToken>());
    }
}
