# Spec: Deduplication & Centralization of Gateway & Shared Utilities

## 1. Status & Context
- **Status**: DRAFT (In Review)
- **Target Microservices/Projects**: `FinanceHub.ApiGateway`, `FinanceHub.Shared.Observability`, `FinanceHub.UnitTests`
- **Objective**: Identify, specify and centralize duplicated patterns flagged by SonarCloud across PR #8 (`feature/api-gateway-bff`).

## 2. Identified Duplication Targets
1. **Target A**: Test Mock HTTP Handlers (`MockHttpMessageHandler` duplicated across test classes in `FinanceHub.UnitTests`).
2. **Target B**: Downstream HTTP Client Error Handling & Payload Deserialization in `ApiGateway` (`AuthConsentServiceClient` & `TransactionAggregatorServiceClient`).
3. **Target C**: RFC 7807 `ProblemDetails` Builder Logic in `Shared.Observability` Exception Mappers.

## 3. Architectural Decisions

### 3.1 Escopo Aprovado
- **Decisão 1**: Centralizar todos os 3 alvos identificados:
  1. `MockHttpMessageHandler` em `FinanceHub.UnitTests/Helpers/MockHttpMessageHandler.cs`.
  2. Execução HTTP downstream padronizada no `ApiGateway` via extension ou base client helper.
  3. Fábrica padronizada de `ProblemDetails` em `FinanceHub.Shared.Observability/Exceptions/Mapping/ProblemDetailsFactory.cs`.

### 3.2 Padrão para Clientes HTTP Downstream (`ApiGateway`)
- **Decisão 2**: Utilizar métodos de extensão estáticos encapsulados em `FinanceHub.ApiGateway/Clients/Extensions/HttpClientDownstreamExtensions.cs`:
  - `SendAndDeserializeAsync<T>(this HttpClient client, HttpRequestMessage request, string serviceName, ILogger logger, CancellationToken ct)`
  - `SendOrThrowAsync(this HttpClient client, HttpRequestMessage request, string serviceName, ILogger logger, CancellationToken ct)`
  - Encapsula: `try/catch (HttpRequestException)`, verificação `!response.IsSuccessStatusCode`, leitura e extração de `errorContent`, e lançamento padronizado de `GatewayDownstreamException(serviceName, ...)`.

### 3.3 Helper de ProblemDetails (`FinanceHub.Shared.Observability`)
- **Decisão 3**: Criar classe utilitária estática `ProblemDetailsBuilder` / `ProblemDetailsFactory` em `FinanceHub.Shared.Observability/Exceptions/Mapping/ProblemDetailsFactory.cs`:
  - Método `Create(int statusCode, string title, string detail, string errorCode, string traceId, string instance)`
  - Elimina duplicação de instanciação e preenchimento de extensões `errorCode` e `traceId` nos mappers (`DomainExceptionMapper`, `InfrastructureExceptionMapper`, `DefaultExceptionMapper`).

### 3.4 Helper Compartilhado para Testes (`FinanceHub.UnitTests`)
- **Decisão 4**: Centralizar `MockHttpMessageHandler` em `FinanceHub.UnitTests/Helpers/MockHttpMessageHandler.cs` acessível por todas as classes de teste unitário de clientes HTTP.

---

## 4. Implementation Plan & Checklist

- [x] **Fase 1: Shared Observability (Target C)**
  - [x] Criar `ProblemDetailsFactory.cs` em `FinanceHub.Shared.Observability/Exceptions/Mapping/`
  - [x] Refatorar `DomainExceptionMapper.cs`, `InfrastructureExceptionMapper.cs` e `DefaultExceptionMapper.cs` para utilizar a factory
- [x] **Fase 2: ApiGateway Downstream Helper (Target B)**
  - [x] Criar `HttpClientDownstreamExtensions.cs` em `FinanceHub.ApiGateway/Clients/Extensions/`
  - [x] Refatorar `AuthConsentServiceClient.cs` para usar as extensões centralizadas
  - [x] Refatorar `TransactionAggregatorServiceClient.cs` para usar as extensões centralizadas
- [x] **Fase 3: Unit Tests Mock Centralization (Target A)**
  - [x] Criar `MockHttpMessageHandler.cs` em `tests/FinanceHub.UnitTests/Helpers/`
  - [x] Refatorar `AuthConsentServiceClientTests.cs` e `TransactionAggregatorServiceClientTests.cs`
- [x] **Fase 4: Validação & Testes**
  - [x] Executar `dotnet build FinanceHub.slnx`
  - [x] Executar `dotnet test FinanceHub.slnx`
  - [x] Garantir 100% de aprovação e zero regressão nos 91 testes existentes

