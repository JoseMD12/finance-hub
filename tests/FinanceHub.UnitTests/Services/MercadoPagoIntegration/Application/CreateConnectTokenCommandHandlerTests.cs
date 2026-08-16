using FinanceHub.MercadoPagoIntegration.Application.Commands.CreateConnectToken;
using FinanceHub.MercadoPagoIntegration.Application.DTOs;
using FinanceHub.MercadoPagoIntegration.Application.Interfaces;
using FinanceHub.MercadoPagoIntegration.Domain.Exceptions;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace FinanceHub.UnitTests.Services.MercadoPagoIntegration.Application;

public class CreateConnectTokenCommandHandlerTests
{
    private readonly IOpenFinanceClient _client = Substitute.For<IOpenFinanceClient>();
    private readonly CreateConnectTokenCommandHandler _handler;

    public CreateConnectTokenCommandHandlerTests()
    {
        _handler = new CreateConnectTokenCommandHandler(_client, NullLogger<CreateConnectTokenCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WithEmptyUserId_ShouldThrowDomainException()
    {
        // Act
        var act = () => _handler.Handle(new CreateConnectTokenCommand(""), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NullOrEmptyMercadoPagoCredentialsDomainException>();
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldReturnConnectToken()
    {
        // Arrange
        _client.CreateConnectTokenAsync("item-1", Arg.Any<CancellationToken>())
            .Returns(new ConnectTokenDto("connect-token-123", DateTime.UtcNow.AddMinutes(30)));

        // Act
        var result = await _handler.Handle(new CreateConnectTokenCommand("user-1", "item-1"), CancellationToken.None);

        // Assert
        result.AccessToken.Should().Be("connect-token-123");
    }
}
