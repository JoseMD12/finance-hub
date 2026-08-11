using Testcontainers.PostgreSql;
using Xunit;

namespace FinanceHub.UnitTests.Fixtures;

public class PostgreSqlTestContainerFixture : IAsyncLifetime
{
    public PostgreSqlContainer Container { get; } = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("financehub_test_db")
        .WithUsername("test_user")
        .WithPassword("test_password")
        .Build();

    public string ConnectionString => Container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await Container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await Container.DisposeAsync();
    }
}
