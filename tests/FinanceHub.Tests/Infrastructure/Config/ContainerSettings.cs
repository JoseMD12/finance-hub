namespace FinanceHub.Tests.Infrastructure.Config;

public record ContainerSettings(
    string PostgresImage = "postgres:16-alpine",
    string PostgresDatabase = "financehub_test_db",
    string PostgresUsername = "test_user",
    string PostgresPassword = "test_password",
    string RabbitMqImage = "rabbitmq:3.12-management-alpine",
    string RabbitMqUsername = "test_guest",
    string RabbitMqPassword = "test_guest"
);
