using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using FinanceHub.ApiGateway.Clients.Extensions;
using FinanceHub.ApiGateway.Exceptions;
using FinanceHub.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace FinanceHub.Tests.ApiGateway.Clients;

public class HttpClientDownstreamExtensionsTests
{
    private readonly ILogger _logger = Substitute.For<ILogger>();

    private sealed record SampleDto(string Name, int Value);

    [Fact]
    public async Task SendAndDeserializeAsync_WhenSuccess_ShouldReturnDeserializedObject()
    {
        // Arrange
        var expected = new SampleDto("Test", 42);
        var handler = new MockHttpMessageHandler
        {
            ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(expected)
            }
        };
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000") };
        var request = new HttpRequestMessage(HttpMethod.Get, "/test");

        // Act
        var result = await client.SendAndDeserializeAsync<SampleDto>(request, "TestService", _logger, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test");
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task SendAndDeserializeAsync_WhenDownstreamFails_ShouldThrowGatewayDownstreamException()
    {
        // Arrange
        var handler = new MockHttpMessageHandler
        {
            ResponseToReturn = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("Boom")
            }
        };
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000") };
        var request = new HttpRequestMessage(HttpMethod.Get, "/test");

        // Act
        var act = async () => await client.SendAndDeserializeAsync<SampleDto>(request, "TestService", _logger, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<GatewayDownstreamException>()
            .WithMessage("*TestService*");
    }
}
