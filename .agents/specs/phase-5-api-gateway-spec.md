# Phase 5 — API Gateway / BFF (`FinanceHub.ApiGateway`) Specification

> **Status**: `Draft — Pending Implementation`
> **Last Updated**: `2026-08-13`
> **Author**: `FinanceHub Architecture Team & User`
> **Depends On**: Phase 2 (AuthConsent ✅), Phase 4 (TransactionAggregator ✅)
> **Phase 3 Note**: Fase 3 (Bank Connectors — lógica de negócio real) está deliberadamente pulada neste momento. O Gateway opera com os serviços internos existentes sem depender dos conectores FAPI reais.

---

## 🎯 Scope & Context

O **`FinanceHub.ApiGateway`** é o **único ponto de entrada externo (BFF — Backend for Frontend)** da plataforma FinanceHub. Ele **não possui banco de dados próprio**, não executa regras de negócio complexas e não acessa DBs de outros serviços diretamente.

### Responsabilidades Primárias:
1. **Proxy & Agregação HTTP**: Rotear e agregar chamadas HTTP para `AuthConsent` e `TransactionAggregator` usando `HttpClient` tipado.
2. **Autenticação JWT Bearer**: Validar tokens JWT emitidos internamente para proteger todos os endpoints públicos do BFF.
3. **Agregação de Dados (Dashboard)**: Endpoint de dashboard que agrega saldo consolidado + consentimentos ativos em uma única resposta, reduzindo round-trips do frontend.
4. **Rate Limiting**: Proteção por IP e por usuário autenticado contra abuso.
5. **Correlação & Observabilidade**: Propagar `traceparent` (OpenTelemetry) em todas as chamadas downstream.
6. **Health Check Agregado**: Verificar saúde dos serviços downstream e retornar status consolidado.

---

## 🏛️ Decisões Arquiteturais Confirmadas

### 1. Estratégia de Comunicação com Serviços Downstream

- **Decisão**: `HttpClient` tipado via `ITypedHttpClient` — **sem YARP ou proxy reverso externo**.
  - Motivo: O Gateway precisa de lógica de agregação customizada (ex: `DashboardAggregation`). Um simples proxy reverso (YARP) não permitiria combinar respostas de múltiplos serviços.
  - Cada serviço downstream tem seu próprio `HttpClient` tipado com configuração de `BaseAddress`, timeout e resiliência via **Polly** (`Retry` + `CircuitBreaker`).
- **Clientes HTTP Tipados**:
  - `IAuthConsentServiceClient` / `AuthConsentServiceClient`
  - `ITransactionAggregatorServiceClient` / `TransactionAggregatorServiceClient`

---

### 2. Autenticação & Autorização JWT

- **Decisão**: Validação de **Bearer JWT** com `Microsoft.AspNetCore.Authentication.JwtBearer`.
  - Todos os endpoints do BFF (exceto `/health` e `/health/detailed`) requerem token JWT válido.
  - O token JWT carrega `sub` (userId), `scope` (ex: `openfinance:read`, `openfinance:write`), `iss` e `exp`.
  - O `userId` é extraído do claim `sub` do token e injetado automaticamente nas chamadas downstream.
  - **Nenhum endpoint público expõe `userId` como query string sem autenticação**.
- **Configuração**: Chave de assinatura JWT carregada via variável de ambiente `JWT_SECRET_KEY` (HS256 para dev, RS256 para produção via KMS). Zero defaults hardcoded.

#### Escopos de Autorização:

| Escopo | Permissão |
|---|---|
| `openfinance:read` | Leitura de saldo, transações e consentimentos |
| `openfinance:write` | Criação e revogação de consentimentos, categorização |
| `openfinance:admin` | Operações administrativas (ingestão interna) |

---

### 3. Endpoints REST do BFF (Contrato Público)

O Gateway **não duplica endpoints internos**. Ele **agrega e protege** o acesso externo. Todos os endpoints são prefixados com `/api/v1/gateway`.

#### Grupo: Dashboard & Saldo

| Método | Endpoint | Auth | Serviços Downstream | Descrição |
|---|---|---|---|---|
| `GET` | `/api/v1/gateway/dashboard` | JWT (`openfinance:read`) | AuthConsent + TransactionAggregator | Agrega consentimentos ativos + saldo consolidado |
| `GET` | `/api/v1/gateway/balances/consolidated` | JWT (`openfinance:read`) | TransactionAggregator | Saldo consolidado + quebra por instituição |

#### Grupo: Transações

| Método | Endpoint | Auth | Serviços Downstream | Descrição |
|---|---|---|---|---|
| `GET` | `/api/v1/gateway/transactions` | JWT (`openfinance:read`) | TransactionAggregator | Extrato paginado com filtros |
| `PATCH` | `/api/v1/gateway/transactions/{id}/category` | JWT (`openfinance:write`) | TransactionAggregator | Categorizar manualmente uma transação |

#### Grupo: Consentimentos

| Método | Endpoint | Auth | Serviços Downstream | Descrição |
|---|---|---|---|---|
| `GET` | `/api/v1/gateway/consents` | JWT (`openfinance:read`) | AuthConsent | Listar consentimentos ativos do usuário autenticado |
| `POST` | `/api/v1/gateway/consents` | JWT (`openfinance:write`) | AuthConsent | Criar novo consentimento bancário |
| `POST` | `/api/v1/gateway/consents/{id}/authorize` | JWT (`openfinance:write`) | AuthConsent | Autorizar consentimento com código OAuth |
| `DELETE` | `/api/v1/gateway/consents/{id}` | JWT (`openfinance:write`) | AuthConsent | Revogar consentimento |

#### Grupo: Saúde & Status

| Método | Endpoint | Auth | Descrição |
|---|---|---|---|
| `GET` | `/health` | Público | Health check simples do Gateway |
| `GET` | `/health/detailed` | Público | Health check agregado com status dos serviços downstream |

---

### 4. Modelo de Resposta de Dashboard (Agregação BFF)

O endpoint `GET /api/v1/gateway/dashboard` combina duas chamadas paralelas com `Task.WhenAll`:

```csharp
// Chamadas paralelas para reduzir latência
var consentTask = _authConsentClient.GetConsentsByUserIdAsync(userId, ct);
var balanceTask = _transactionAggregatorClient.GetConsolidatedBalanceAsync(userId, ct);
await Task.WhenAll(consentTask, balanceTask);
```

**DTOs de Resposta** (`DTOs/DashboardResponseDto.cs`):

```csharp
public record DashboardResponseDto(
    string UserId,
    decimal TotalBalanceBrl,
    IEnumerable<AccountBalanceSummaryDto> AccountBalances,
    IEnumerable<ActiveConsentSummaryDto> ActiveConsents,
    DateTime GeneratedAtUtc);

public record AccountBalanceSummaryDto(
    string InstitutionId,
    string AccountNumber,
    decimal Amount,
    string Currency,
    DateTime LastUpdatedAtUtc);

public record ActiveConsentSummaryDto(
    Guid ConsentId,
    string InstitutionId,
    string Status,
    DateTime? ExpiresAtUtc);
```

---

### 5. Resiliência via Polly (Retry + Circuit Breaker)

- **Retry Policy**: 3 tentativas com backoff exponencial (1s → 2s → 4s) para erros `5xx` e timeout.
- **Circuit Breaker**: Abre após 5 falhas consecutivas em 30 segundos. Probe de meio-aberto após 15s.
- **Timeout Policy**: 10 segundos por chamada downstream (configurável via `.env`).
- Políticas centralizadas em `Resilience/ResiliencePolicies.cs`.

---

### 6. Rate Limiting

- **Decisão**: `Microsoft.AspNetCore.RateLimiting` (built-in .NET 8+/10).
- **Por IP (anônimo)**: 30 req/min (Fixed Window) nos endpoints de health/público.
- **Por UserId (autenticado)**: 120 req/min (Sliding Window) nos endpoints protegidos.
- Configurado exclusivamente em `DependencyInjection.cs`.
- Retorna `429 Too Many Requests` com header `Retry-After` em segundos.

---

### 7. Hierarquia de Exceções & RFC 7807

O Gateway não possui lógica de domínio — apenas trata erros de comunicação HTTP.

| Exceção Gateway | Condição | Status HTTP | ErrorCode |
|---|---|---|---|
| `GatewayDownstreamException` | Serviço downstream retornou erro não recuperável | 502 Bad Gateway | `DOWNSTREAM_ERROR` |
| `GatewayTimeoutException` | Timeout na chamada downstream | 504 Gateway Timeout | `DOWNSTREAM_TIMEOUT` |
| `GatewayCircuitOpenException` | Circuit Breaker aberto | 503 Service Unavailable | `CIRCUIT_OPEN` |

Tratamento global via `GlobalExceptionHandler.cs` implementando `IExceptionHandler` do .NET 10. Zero `try/catch` manual nos endpoints.

---

## 📂 Mapeamento Detalhado de Arquivos

O `FinanceHub.ApiGateway` é um projeto **monolítico de entrada única** — não segue Clean Architecture com múltiplas camadas. Segue estrutura simplificada de BFF:

```
src/Services/ApiGateway/FinanceHub.ApiGateway/
├── FinanceHub.ApiGateway.csproj
├── Program.cs                                    ← Startup e pipeline HTTP
├── DependencyInjection.cs                        ← Registro global de todos os serviços
├── Endpoints/
│   ├── DashboardEndpoints.cs                     ← GET /gateway/dashboard + /balances/consolidated
│   ├── TransactionGatewayEndpoints.cs            ← GET/PATCH /gateway/transactions
│   └── ConsentGatewayEndpoints.cs                ← GET/POST/DELETE /gateway/consents
├── Clients/
│   ├── IAuthConsentServiceClient.cs              ← Interface do cliente HTTP tipado (arquivo separado)
│   ├── AuthConsentServiceClient.cs               ← Implementação com HttpClient (arquivo separado)
│   ├── ITransactionAggregatorServiceClient.cs    ← Interface do cliente HTTP tipado (arquivo separado)
│   └── TransactionAggregatorServiceClient.cs     ← Implementação com HttpClient (arquivo separado)
├── DTOs/
│   ├── DashboardResponseDto.cs
│   ├── GatewayTransactionDto.cs
│   └── GatewayConsentDto.cs
├── Exceptions/
│   ├── GatewayDomainException.cs                 ← Base abstrata
│   ├── GatewayDownstreamException.cs
│   ├── GatewayTimeoutException.cs
│   └── GatewayCircuitOpenException.cs
├── Middleware/
│   └── GlobalExceptionHandler.cs                 ← IExceptionHandler RFC 7807
├── Resilience/
│   └── ResiliencePolicies.cs                     ← Políticas Polly centralizadas
├── Constants/
│   └── GatewayConstants.cs                       ← Zero magic strings
├── appsettings.json
├── appsettings.Development.json
└── Properties/
    └── launchSettings.json
```

---

## ⚙️ Variáveis de Ambiente (`.env`)

```env
# JWT
JWT_SECRET_KEY=<mínimo 32 caracteres, gerado via CSPRNG>
JWT_ISSUER=https://financehub.local
JWT_AUDIENCE=financehub-gateway
JWT_EXPIRY_MINUTES=60

# URLs dos Serviços Downstream
AUTH_CONSENT_BASE_URL=http://localhost:5001
TRANSACTION_AGGREGATOR_BASE_URL=http://localhost:5002

# Timeouts e Resiliência (em segundos)
DOWNSTREAM_TIMEOUT_SECONDS=10
CIRCUIT_BREAKER_FAILURE_THRESHOLD=5
CIRCUIT_BREAKER_DURATION_SECONDS=30

# Rate Limiting
RATE_LIMIT_ANONYMOUS_PER_MINUTE=30
RATE_LIMIT_AUTHENTICATED_PER_MINUTE=120

# Observabilidade
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
ASPNETCORE_URLS=http://localhost:5050
```

---

## 📦 Dependências NuGet (`.csproj`)

```xml
<ItemGroup>
  <PackageReference Include="DotNetEnv" Version="3.1.1" />
  <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.*" />
  <PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="*" />
  <PackageReference Include="Microsoft.Extensions.Diagnostics.HealthChecks" Version="10.*" />
  <PackageReference Include="AspNetCore.HealthChecks.Uris" Version="*" />
  <ProjectReference Include="..\..\..\Shared\FinanceHub.Shared.Observability\FinanceHub.Shared.Observability.csproj" />
</ItemGroup>
```

---

## 🧪 Plano de Testes TDD (Upfront Test Cases — Red → Green → Refactor)

Projeto de testes: `tests/FinanceHub.UnitTests/ApiGateway/`

### Bateria 1: Clientes HTTP Tipados (Unit Tests — NSubstitute + MockHttp)

- 🔴 `AuthConsentServiceClient_GetConsentsByUserId_DeveRetornarListaDeConsentimentos`
- 🔴 `AuthConsentServiceClient_QuandoDownstreamRetorna404_DeveLancarGatewayDownstreamException`
- 🔴 `TransactionAggregatorServiceClient_GetConsolidatedBalance_DeveRetornarDtoAgregado`
- 🔴 `TransactionAggregatorServiceClient_QuandoTimeout_DeveLancarGatewayTimeoutException`

### Bateria 2: Agregação do Dashboard (Unit Tests)

- 🔴 `DashboardEndpoint_DeveExecutarChamadasParalelasComTaskWhenAll`
- 🔴 `DashboardEndpoint_QuandoBalanceServiceFalha_DevePropagar502BadGateway`
- 🔴 `DashboardEndpoint_QuandoConsentServiceFalha_DevePropagar502BadGateway`

### Bateria 3: Autenticação JWT (Integration Tests — WebApplicationFactory)

- 🔴 `GatewayEndpoints_SemToken_DeveRetornar401Unauthorized`
- 🔴 `GatewayEndpoints_ComTokenValido_DeveRotearParaDownstream`
- 🔴 `GatewayEndpoints_ComTokenExpirado_DeveRetornar401Unauthorized`
- 🔴 `GatewayEndpoints_ComEscopoInsuficiente_DeveRetornar403Forbidden`

### Bateria 4: Rate Limiting (Integration Tests)

- 🔴 `RateLimit_QuandoExcedeLimiteAutenticado_DeveRetornar429ComRetryAfterHeader`

### Bateria 5: Health Checks (Integration Tests)

- 🔴 `HealthDetailed_QuandoDownstreamSaudavel_DeveRetornar200Healthy`
- 🔴 `HealthDetailed_QuandoDownstreamIndisponivel_DeveRetornar503Degraded`

---

## 🔄 Sequência de Implementação

```text
[1] Constants/GatewayConstants.cs          (zero magic strings — primeiro)
[2] Exceptions/ + GlobalExceptionHandler   (RFC 7807 tratamento global)
[3] Resilience/ResiliencePolicies.cs       (Polly retry + circuit breaker)
[4] Clients/                               (IAuthConsentServiceClient + AuthConsentServiceClient)
                                           (ITransactionAggregatorServiceClient + TransactionAggregatorServiceClient)
[5] DTOs/                                  (DashboardResponseDto, GatewayTransactionDto, GatewayConsentDto)
[6] Endpoints/                             (DashboardEndpoints, TransactionGatewayEndpoints, ConsentGatewayEndpoints)
[7] DependencyInjection.cs + Program.cs   (JWT auth, rate limiting, health checks, HttpClients)
[8] Testes (TDD — Baterias 1-5)
[9] Validação E2E com curl (todos endpoints + cenários de erro)
```

---

## 🔐 Diretrizes de Segurança Específicas do Gateway

1. **Nunca logar tokens JWT** — redação obrigatória via `FinanceHub.Shared.Observability` (LGPD).
2. **Nunca repassar o token JWT raw** nas chamadas downstream — extrair `userId` do claim `sub` e usá-lo nas requests internas.
3. **Nunca expor detalhes internos de erro** do downstream ao cliente externo — encapsular em `GatewayDownstreamException`.
4. **Headers de segurança obrigatórios**: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`.
5. **Fail-fast no startup** para qualquer variável de ambiente ausente (`JWT_SECRET_KEY`, `AUTH_CONSENT_BASE_URL`, `TRANSACTION_AGGREGATOR_BASE_URL`).

---

## ✅ Definition of Done (DoD) — Phase 5

- [ ] Todos os endpoints do BFF implementados e documentados
- [ ] Autenticação JWT funcionando (`401` sem token, `403` sem escopo)
- [ ] Rate limiting configurado e testado (`429` ao exceder limites)
- [ ] Polly Retry + Circuit Breaker configurados e testados
- [ ] Health check agregado (`/health/detailed`) retornando status de todos os downstream
- [ ] `GlobalExceptionHandler` RFC 7807 tratando todos os cenários de erro do Gateway
- [ ] Zero magic strings — tudo centralizado em `GatewayConstants`
- [ ] `DependencyInjection.cs` exclusivo registrando todos os serviços
- [ ] Todas as variáveis de ambiente via `.env` com fail-fast no startup
- [ ] Interfaces e implementações em arquivos `.cs` separados (Regra 13)
- [ ] ≥ 80% de cobertura de testes unitários
- [ ] Validação E2E com `curl` cobrindo fluxo completo
- [ ] Commit: `feat(gateway): implement API Gateway BFF with JWT auth, Polly resilience and dashboard aggregation`
