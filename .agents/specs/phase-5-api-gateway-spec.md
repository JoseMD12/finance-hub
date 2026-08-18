# Phase 5 — API Gateway / BFF (`FinanceHub.ApiGateway`) Specification

> **Status**: `Completed ✅`
> **Last Updated**: `2026-08-13`
> **Author**: `FinanceHub Architecture Team & User`
> **Depends On**: Phase 2 (PluggyIntegration ✅), Phase 4 (TransactionAggregator ✅)
> **Phase 3 Note**: O Gateway opera conectando `PluggyIntegration` e `TransactionAggregator` via clientes HTTP tipados com resiliência.

---

## 🎯 Scope & Context

O **`FinanceHub.ApiGateway`** é o **único ponto de entrada externo (BFF — Backend for Frontend)** da plataforma FinanceHub. Ele **não possui banco de dados próprio**, não executa regras de negócio complexas e não acessa DBs de outros serviços diretamente.

### Responsabilidades Primárias:
1. **Proxy & Agregação HTTP**: Rotear e agregar chamadas HTTP para `PluggyIntegration` e `TransactionAggregator` usando `HttpClient` tipado.
2. **Autenticação JWT Bearer**: Validar tokens JWT emitidos internamente para proteger todos os endpoints públicos do BFF.
3. **Agregação de Dados (Dashboard)**: Endpoint de dashboard que agrega saldo consolidado + movimentações ativas em uma única resposta, reduzindo round-trips do frontend.
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
  - `IPluggyIntegrationServiceClient` / `PluggyIntegrationServiceClient`
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
| `openfinance:read` | Leitura de saldo, transações e extratos |
| `openfinance:write` | Sincronização de contas e categorização |
| `openfinance:admin` | Operações administrativas (ingestão interna) |

---

## 3. Endpoints REST do BFF (Contrato Público)

O Gateway **não duplica endpoints internos**. Ele **agrega e protege** o acesso externo. Todos os endpoints são prefixados com `/api/v1/gateway`.

#### Grupo: Dashboard & Saldo

| Método | Endpoint | Auth | Serviços Downstream | Descrição |
|---|---|---|---|---|
| `GET` | `/api/v1/gateway/dashboard` | JWT (`openfinance:read`) | PluggyIntegration + TransactionAggregator | Agrega contas conectadas + saldo consolidado |
| `GET` | `/api/v1/gateway/balances/consolidated` | JWT (`openfinance:read`) | TransactionAggregator | Saldo consolidado + quebra por instituição |

#### Grupo: Transações

| Método | Endpoint | Auth | Serviços Downstream | Descrição |
|---|---|---|---|---|
| `GET` | `/api/v1/gateway/transactions` | JWT (`openfinance:read`) | TransactionAggregator | Extrato paginado com filtros |
| `PATCH` | `/api/v1/gateway/transactions/{id}/category` | JWT (`openfinance:write`) | TransactionAggregator | Categorizar manualmente uma transação |

#### Grupo: Sincronização Pluggy Open Finance

| Método | Endpoint | Auth | Serviços Downstream | Descrição |
|---|---|---|---|---|
| `POST` | `/api/v1/gateway/sync/pluggy` | JWT (`openfinance:write`) | PluggyIntegration | Sincroniza extratos e faturas das contas conectadas |

#### Grupo: Saúde & Status

| Método | Endpoint | Auth | Descrição |
|---|---|---|---|
| `GET` | `/health` | Público | Health check simples do Gateway |
| `GET` | `/health/detailed` | Público | Health check agregado com status dos serviços downstream |

---

## 4. Modelo de Resposta de Dashboard (Agregação BFF)

O endpoint `GET /api/v1/gateway/dashboard` combina duas chamadas paralelas com `Task.WhenAll`:

```csharp
// Chamadas paralelas para reduzir latência
var pluggyTask = _pluggyClient.GetPluggyAccountsAsync(userId, ct);
var balanceTask = _transactionAggregatorClient.GetConsolidatedBalanceAsync(userId, ct);
await Task.WhenAll(pluggyTask, balanceTask);
```

---

## ✅ Definition of Done (DoD) — Phase 5

- [x] Todos os endpoints do BFF implementados e documentados
- [x] Autenticação JWT funcionando (`401` sem token, `403` sem escopo)
- [x] Rate limiting configurado e testado (`429` ao exceder limites)
- [x] Polly Retry + Circuit Breaker configurados e testados
- [x] Health check agregado (`/health/detailed`) retornando status de todos os downstream
- [x] `GlobalExceptionHandler` RFC 7807 tratando todos os cenários de erro do Gateway
- [x] Zero magic strings — tudo centralizado em `GatewayConstants`
- [x] `DependencyInjection.cs` exclusivo registrando todos os serviços
- [x] Todas as variáveis de ambiente via `.env` com fail-fast no startup
- [x] Interfaces e implementações em arquivos `.cs` separados (Regra 13)
- [x] ≥ 80% de cobertura de testes unitários
- [x] Validação E2E com `curl` cobrindo fluxo completo
- [x] Commits separados por feature em branch dedicada (`feature/api-gateway-bff`)
