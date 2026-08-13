using Xunit;

namespace FinanceHub.UnitTests.Infrastructure;

[CollectionDefinition("IntegrationTests")]
public class IntegrationTestCollection : ICollectionFixture<CustomWebApplicationFactory<FinanceHub.ApiGateway.Program>>
{
    // Esta classe não contém código. 
    // Ela serve para o xUnit compartilhar a Factory (e os containers PostgreSQL + RabbitMQ).
}
