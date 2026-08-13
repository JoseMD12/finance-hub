using FinanceHub.AuthConsent.Application.Commands.RevokeConsent;
using FinanceHub.AuthConsent.Application.Exceptions;
using FinanceHub.AuthConsent.Application.Interfaces;
using FinanceHub.AuthConsent.Domain.Constants;
using FinanceHub.AuthConsent.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Xunit;

namespace FinanceHub.UnitTests.AuthConsent.Application;

public class RevokeConsentCommandHandlerTests
{
    private readonly IBankConsentRepository _repository = Substitute.For<IBankConsentRepository>();
    private readonly FakeTimeProvider _timeProvider = new();

    [Fact]
    public async Task Handle_ComConsentimentoExistente_DeveRevogarEAtualizarNoRepositorio()
    {
        var consent = BankConsent.Request("user-123", BankIdentifiers.Itau, "ext-1", _timeProvider);
        _repository.GetByIdAsync(consent.Id, Arg.Any<CancellationToken>()).Returns(consent);

        var handler = new RevokeConsentCommandHandler(_repository, _timeProvider);
        var command = new RevokeConsentCommand(consent.Id);

        await handler.Handle(command, CancellationToken.None);

        consent.Status.Should().Be(ConsentStatus.Revoked);
        await _repository.Received(1).UpdateAsync(consent, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ComConsentimentoInexistente_DeveLancarConsentNotFoundDomainException()
    {
        var unknownId = Guid.NewGuid();
        _repository.GetByIdAsync(unknownId, Arg.Any<CancellationToken>()).Returns((BankConsent?)null);

        var handler = new RevokeConsentCommandHandler(_repository, _timeProvider);
        var command = new RevokeConsentCommand(unknownId);

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConsentNotFoundDomainException>();
    }
}
