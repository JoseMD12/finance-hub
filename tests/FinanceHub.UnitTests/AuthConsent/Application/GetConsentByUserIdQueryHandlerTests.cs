using FinanceHub.AuthConsent.Application.Queries.GetConsentByUserId;
using FinanceHub.AuthConsent.Application.Interfaces;
using FinanceHub.AuthConsent.Domain.Constants;
using FinanceHub.AuthConsent.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Xunit;

namespace FinanceHub.UnitTests.AuthConsent.Application;

public class GetConsentByUserIdQueryHandlerTests
{
    private readonly IBankConsentRepository _repository = Substitute.For<IBankConsentRepository>();
    private readonly FakeTimeProvider _timeProvider = new();

    [Fact]
    public async Task Handle_ComUserIdValido_DeveRetornarListaDeConsentDtos()
    {
        var consent1 = BankConsent.Request("user-123", BankIdentifiers.Itau, "ext-1", _timeProvider);
        var consent2 = BankConsent.Request("user-123", BankIdentifiers.MercadoPago, "ext-2", _timeProvider);

        _repository.GetByUserIdAsync("user-123", Arg.Any<CancellationToken>())
                   .Returns(new List<BankConsent> { consent1, consent2 });

        var handler = new GetConsentByUserIdQueryHandler(_repository);
        var query = new GetConsentByUserIdQuery("user-123");

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(r => r.InstitutionId).Should().Contain([BankIdentifiers.Itau, BankIdentifiers.MercadoPago]);
    }
}
