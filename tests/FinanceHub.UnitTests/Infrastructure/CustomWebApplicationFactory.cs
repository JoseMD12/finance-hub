using System.Collections.Generic;
using System.Threading.Tasks;
using FinanceHub.UnitTests.Infrastructure.Config;
using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace FinanceHub.UnitTests.Infrastructure;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>, IAsyncLifetime
    where TProgram : class
{
    private static readonly ContainerSettings Settings = new();

    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage(Settings.PostgresImage)
        .WithDatabase(Settings.PostgresDatabase)
        .WithUsername(Settings.PostgresUsername)
        .WithPassword(Settings.PostgresPassword)
        .WithCleanUp(true)
        .Build();

    private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder()
        .WithImage(Settings.RabbitMqImage)
        .WithUsername(Settings.RabbitMqUsername)
        .WithPassword(Settings.RabbitMqPassword)
        .WithCleanUp(true)
        .Build();

    public string PostgresConnectionString => _postgresContainer.GetConnectionString();
    public string RabbitMqHost => _rabbitMqContainer.Hostname;
    public string RabbitMqPort => _rabbitMqContainer.GetMappedPublicPort(5672).ToString();
    public string RabbitMqUsername => Settings.RabbitMqUsername;
    public string RabbitMqPassword => Settings.RabbitMqPassword;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:AuthConsentDb", PostgresConnectionString);
        builder.UseSetting("ConnectionStrings:TransactionAggregatorDb", PostgresConnectionString);
        builder.UseSetting("ConnectionStrings:DefaultConnection", PostgresConnectionString);
        builder.UseSetting("RabbitMQ:Host", RabbitMqHost);
        builder.UseSetting("RabbitMQ:Port", RabbitMqPort);
        builder.UseSetting("RabbitMQ:Username", RabbitMqUsername);
        builder.UseSetting("RabbitMQ:Password", RabbitMqPassword);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:AuthConsentDb", PostgresConnectionString },
                { "ConnectionStrings:TransactionAggregatorDb", PostgresConnectionString },
                { "ConnectionStrings:DefaultConnection", PostgresConnectionString },
                { "RabbitMQ:Host", RabbitMqHost },
                { "RabbitMQ:Port", RabbitMqPort },
                { "RabbitMQ:Username", RabbitMqUsername },
                { "RabbitMQ:Password", RabbitMqPassword }
            });
        });
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        await _rabbitMqContainer.StartAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgresContainer.StopAsync();
        await _rabbitMqContainer.StopAsync();
        await base.DisposeAsync();
    }
}
