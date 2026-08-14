---
name: run-tdd
description: Mandatory TDD Red-Green-Refactor testing skill for FinanceHub microservices (.NET 10). Covers unit, integration, and contract testing patterns for all layers using xUnit, FluentAssertions, NSubstitute, and Testcontainers. Enforces 80% minimum coverage per service.
---

# .NET 10 TDD & Testing Skill — FinanceHub

## ⚡ Trigger / Slash Command
```bash
/run-tdd
```

Executa obrigatoriamente o ciclo TDD: **1. Red** (Escrever teste e rodar até falhar) → **2. Green** (Escrever código mínimo de produção) → **3. Refactor** (Ajustar arquitetura sem quebrar testes).

---

## 🗺️ Mapa de Cobertura por Serviço

Cada microsserviço deve ter testes nas seguintes camadas:

```
tests/FinanceHub.UnitTests/
  ├── Shared/                    ← Testes dos módulos compartilhados
  ├── AuthConsent/               ← Testes do serviço de consentimento
  ├── ItauIntegration/           ← Testes do conector Itaú
  ├── MercadoPagoIntegration/    ← Testes do conector Mercado Pago
  ├── TransactionAggregator/     ← Testes do agregador de transações
  └── ApiGateway/                ← Testes do BFF / Gateway
```

**Cobertura mínima exigida: 80% por microsserviço.**

---

## 📦 Stack de Testes

| Biblioteca | Versão | Propósito |
|------------|--------|-----------|
| `xUnit` | 2.9+ | Framework de testes |
| `FluentAssertions` | 8.x | Asserções expressivas |
| `NSubstitute` | 5.x | Mocking de dependências |
| `Testcontainers` | 3.x | PostgreSQL/RabbitMQ reais em containers |
| `Microsoft.AspNetCore.Mvc.Testing` | 10.x | Testes de integração de endpoints |
| `Bogus` | 35.x | Geração de dados realistas (CPF, transações, etc.) |

---

## 🧪 Padrões de Teste por Camada

### 1. Camada de Domínio (`Domain`)

Teste entidades, value objects, regras de negócio e domain events **sem dependências externas**.

```csharp
// Arrange: instanciar a entidade com dados válidos
// Act: executar o método de domínio
// Assert: verificar estado e eventos emitidos

public class TransacaoTests
{
    [Fact]
    public void Transacao_ComValorNegativo_DeveLancarDomainException()
    {
        // Arrange & Act
        var act = () => new Transacao(valor: -100m, descricao: "PIX inválido");

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("*valor*");
    }
}
```

### 2. Camada de Aplicação (`Application`)

Teste Use Cases / Handlers com mocks (NSubstitute) para repositórios e serviços externos.

```csharp
public class SyncTransacoesHandlerTests
{
    private readonly ITransacaoRepository _repo = Substitute.For<ITransacaoRepository>();
    private readonly IBankConnector _connector = Substitute.For<IBankConnector>();
    private readonly SyncTransacoesHandler _handler;

    public SyncTransacoesHandlerTests()
    {
        _handler = new SyncTransacoesHandler(_repo, _connector);
    }

    [Fact]
    public async Task Handle_DevePublicarTransactionIngested_QuandoBancoRetornarTransacoes()
    {
        // Arrange
        _connector.GetTransacoesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                  .Returns([FakeTransacao.Build()]);

        // Act
        await _handler.Handle(new SyncTransacoesCommand("acc-001"), CancellationToken.None);

        // Assert
        await _repo.Received(1).AddAsync(Arg.Any<Transacao>(), Arg.Any<CancellationToken>());
    }
}
```

### 3. Camada de Infraestrutura (`Infrastructure`) — Testcontainers

Teste repositórios EF Core contra um banco **PostgreSQL real** em container Docker.

```csharp
public class TransacaoRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        // Apply EF Core migrations
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact]
    public async Task AddAsync_DevePersistirTransacao_ERetornarComId()
    {
        // Arrange
        var repo = BuildRepository(_postgres.GetConnectionString());
        var transacao = FakeTransacao.Build();

        // Act
        await repo.AddAsync(transacao, CancellationToken.None);
        var encontrada = await repo.GetByIdAsync(transacao.Id, CancellationToken.None);

        // Assert
        encontrada.Should().NotBeNull();
        encontrada!.Id.Should().Be(transacao.Id);
    }
}
```

### 4. Camada de API (`Api`) — WebApplicationFactory

Teste endpoints Minimal API com servidor HTTP em memória.

```csharp
public class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GET_Health_DeveRetornar200ComStatusHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body!.Status.Should().Be("Healthy");
    }
}
```

### 5. Testes de Deduplicação (TransactionAggregator)

Teste o algoritmo SHA-256 determinístico de deduplicação.

```csharp
[Fact]
public void DeduplicationHashService_MesmaTransacao_DeveGerarHashIdentico()
{
    var svc = new DeduplicationHashService();
    var t1 = new TransactionIngested(/* ... */);
    var t2 = t1 with { RawPayloadJson = "diferente" }; // mesmo conteúdo financeiro

    svc.ComputeHash(t1).Should().Be(svc.ComputeHash(t2));
}

[Fact]
public void DeduplicationHashService_TransacoesDiferentes_DeveGerarHashDistinto()
{
    var svc = new DeduplicationHashService();
    var t1 = new TransactionIngested(Amount: 100m, /* ... */);
    var t2 = new TransactionIngested(Amount: 200m, /* ... */);

    svc.ComputeHash(t1).Should().NotBe(svc.ComputeHash(t2));
}
```

---

## 🏭 Fábricas de Dados Falsos (Bogus)

Crie fábricas centralizadas para dados de teste realistas e reutilizáveis.

```csharp
// tests/FinanceHub.UnitTests/Factories/FakeTransactionIngested.cs
public static class FakeTransactionIngested
{
    public static TransactionIngested Build(string source = "Itau") =>
        new Faker<TransactionIngested>()
            .CustomInstantiator(f => new TransactionIngested(
                IngestionId: Guid.NewGuid(),
                Source: source,
                AccountId: f.Random.AlphaNumeric(8),
                BankTransactionId: f.Random.AlphaNumeric(16),
                Amount: f.Finance.Amount(1, 5000),
                TransactionDate: f.Date.Recent(30),
                Description: f.Commerce.ProductName(),
                Currency: "BRL",
                RawPayloadJson: "{}",
                OccurredAtUtc: DateTime.UtcNow
            )).Generate();
}
```

---

## 🔒 Regras de Segurança em Testes

1. **Nunca use credenciais reais** (tokens, senhas, CPFs reais) em testes.
2. **Não commite arquivos `.pfx`** — use `NSubstitute` para mockar `ICertificateProvider`.
3. **Redija PII nos logs de teste** — configure Serilog com `MinimumLevel.Warning` nos testes.
4. **Isole bancos de dados** — cada teste de infraestrutura usa um container separado via `IAsyncLifetime`.

---

## 📊 Comandos Úteis

```bash
# Rodar todos os testes
dotnet test

# Com cobertura de código (Coverlet)
dotnet test --collect:"XPlat Code Coverage"

# Filtrar por serviço
dotnet test --filter "FullyQualifiedName~AuthConsent"

# Filtrar por categoria
dotnet test --filter "Category=Integration"
```

---

## ✅ Checklist de Qualidade por PR

Antes de abrir qualquer PR, verifique:
- [ ] `dotnet build` sem erros
- [ ] `dotnet test` — 100% dos testes passando
- [ ] Cobertura ≥ 80% na camada `Application` do serviço afetado
- [ ] Nenhum dado real (CPF, token, senha) em testes
- [ ] Novos handlers/use cases com pelo menos 1 teste de sucesso e 1 de falha
