using System.IO;

using FinanceHub.ApiGateway.Exceptions;
using FinanceHub.ApiGateway.Middleware;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using NSubstitute;

using Xunit;

namespace FinanceHub.UnitTests.ApiGateway.Middleware;

public class GlobalExceptionHandlerTests
{
    private readonly ILogger<GlobalExceptionHandler> _logger = Substitute.For<ILogger<GlobalExceptionHandler>>();

    [Fact]
    public async Task TryHandleAsync_WithGatewayDownstreamException_ShouldSet502Status()
    {
        // Arrange
        var handler = new GlobalExceptionHandler(_logger);
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        var exception = new GatewayDownstreamException("TestService", "Falha de conexão");

        // Act
        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        handled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status502BadGateway);
        httpContext.Response.ContentType.Should().Contain("application/problem+json");
    }
}
