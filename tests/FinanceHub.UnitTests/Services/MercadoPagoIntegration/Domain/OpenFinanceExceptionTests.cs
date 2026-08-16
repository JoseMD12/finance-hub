using FinanceHub.MercadoPagoIntegration.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace FinanceHub.UnitTests.Services.MercadoPagoIntegration.Domain;

public class OpenFinanceExceptionTests
{
    [Fact]
    public void OpenFinanceAuthenticationDomainException_ShouldHave401StatusCode()
    {
        var ex = new OpenFinanceAuthenticationDomainException();
        ex.StatusCode.Should().Be(401);
        ex.ErrorCode.Should().Be("OPENFINANCE_UNAUTHORIZED");
    }

    [Fact]
    public void OpenFinanceItemNotFoundDomainException_ShouldHave404StatusCode()
    {
        var ex = new OpenFinanceItemNotFoundDomainException("item-999");
        ex.StatusCode.Should().Be(404);
        ex.ErrorCode.Should().Be("OPENFINANCE_ITEM_NOT_FOUND");
        ex.Message.Should().Contain("item-999");
    }

    [Fact]
    public void OpenFinanceConsentRevokedDomainException_ShouldHave409StatusCode()
    {
        var ex = new OpenFinanceConsentRevokedDomainException("REVOKED");
        ex.StatusCode.Should().Be(409);
        ex.ErrorCode.Should().Be("OPENFINANCE_CONSENT_REVOKED");
    }

    [Fact]
    public void OpenFinanceRateLimitExceededDomainException_ShouldHave429StatusCode()
    {
        var ex = new OpenFinanceRateLimitExceededDomainException(30);
        ex.StatusCode.Should().Be(429);
        ex.ErrorCode.Should().Be("OPENFINANCE_RATE_LIMIT_EXCEEDED");
        ex.RetryAfterSeconds.Should().Be(30);
    }

    [Fact]
    public void OpenFinanceServiceDomainException_ShouldHave502StatusCode()
    {
        var ex = new OpenFinanceServiceDomainException("Erro upstream");
        ex.StatusCode.Should().Be(502);
        ex.ErrorCode.Should().Be("OPENFINANCE_GATEWAY_ERROR");
    }
}
