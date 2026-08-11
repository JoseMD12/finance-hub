using FinanceHub.UnitTests.Fixtures;
using FluentAssertions;
using Npgsql;
using Xunit;

namespace FinanceHub.UnitTests.Integration;

public class PostgresContainerIntegrationTests : IClassFixture<PostgreSqlTestContainerFixture>
{
    private readonly PostgreSqlTestContainerFixture _fixture;

    public PostgresContainerIntegrationTests(PostgreSqlTestContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PostgresContainer_ShouldExecuteQueriesInIsolatedEnvironment()
    {
        // Arrange
        await using var conn = new NpgsqlConnection(_fixture.ConnectionString);
        await conn.OpenAsync();

        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS test_ledger (
                    id UUID PRIMARY KEY,
                    amount NUMERIC(18,2) NOT NULL,
                    description TEXT NOT NULL
                );";
            await cmd.ExecuteNonQueryAsync();
        }

        var testId = Guid.NewGuid();

        // Act
        await using (var insertCmd = conn.CreateCommand())
        {
            insertCmd.CommandText = "INSERT INTO test_ledger (id, amount, description) VALUES (@id, @amount, @description);";
            insertCmd.Parameters.AddWithValue("id", testId);
            insertCmd.Parameters.AddWithValue("amount", 299.99m);
            insertCmd.Parameters.AddWithValue("description", "Supermercado Teste");
            await insertCmd.ExecuteNonQueryAsync();
        }

        // Assert
        await using (var selectCmd = conn.CreateCommand())
        {
            selectCmd.CommandText = "SELECT amount, description FROM test_ledger WHERE id = @id;";
            selectCmd.Parameters.AddWithValue("id", testId);
            await using var reader = await selectCmd.ExecuteReaderAsync();

            (await reader.ReadAsync()).Should().BeTrue();
            reader.GetDecimal(0).Should().Be(299.99m);
            reader.GetString(1).Should().Be("Supermercado Teste");
        }
    }
}
