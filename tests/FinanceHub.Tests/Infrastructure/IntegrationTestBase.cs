using System;
using System.Net.Http;
using Xunit;

namespace FinanceHub.Tests.Infrastructure;

[Collection("IntegrationTests")]
public abstract class IntegrationTestBase<TProgram> : IDisposable
    where TProgram : class
{
    protected readonly CustomWebApplicationFactory<TProgram> Factory;
    protected readonly HttpClient Client;
    protected readonly IServiceProvider Services;

    protected IntegrationTestBase(CustomWebApplicationFactory<TProgram> factory)
    {
        Factory = factory;
        Client = Factory.CreateClient();
        Services = Factory.Services;
    }

    public void Dispose()
    {
        Client.Dispose();
        GC.SuppressFinalize(this);
    }
}
