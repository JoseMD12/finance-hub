# 📋 Especificação Técnica: Microsserviço FinanceHub.PluggyIntegration

Especificação técnica arquitetural para o conector de sincronização bancária via Open Finance B2C (`meu.pluggy.ai`), cobrindo **Banco Inter, Itaú e Mercado Pago**.

---

## 🎯 1. Visão Geral e Fronteiras do Microsserviço
* **Nome do Serviço**: `FinanceHub.PluggyIntegration`
* **Objetivo**: Extrair contas correntes, faturas de cartão de crédito e histórico de transações a partir da API `https://my-api.pluggy.ai`, publicando eventos granulares de domínio via MassTransit Transactional Outbox.
* **Instituições Atendidas**: Banco Inter, Banco Itaú, Mercado Pago.
* **Porta do Serviço (Kestrel)**: `5056` (ou porta dedicada configurada via `.env`).

---

## 🏛️ 2. Decisões Arquiteturais Consolidadas

| Item | Decisão | Racional / Justificativa |
| :--- | :--- | :--- |
| **Topologia** | Microsserviço Dedicado em `src/Services/PluggyIntegration/` | Clean Architecture + DDD (`Domain`, `Application`, `Infrastructure`, `Api`), isolado e desacoplado. |
| **Disparo** | On-Demand via Endpoint no `ApiGateway` (`POST /api/v1/gateway/sync/pluggy`) | Permite atualização sob demanda pelo frontend com retorno em tempo real do total sincronizado. |
| **Eventos** | Granulares: `TransactionIngested` (Conta Corrente) e `InvoiceItemIngested` (Cartão) | Idempotência via `HashUnico`, rastreabilidade e processamento assíncrono seguro. |
| **Categorias** | Dicionário Canônico Centralizado (`PluggyCategoryMapper`) | Normaliza termos da Pluggy em inglês (`Eating out`, `Groceries`, etc.) para categorias oficiais em português. |
| **Resiliência** | Polly Resilience Pipeline (Retry com Jitter, Timeout 15s, Circuit Breaker) | Protege contra falhas transitórias de rede e respostas HTTP 429/5xx sem sobrecarregar a API. |

---

## 🔐 3. Segurança, LGPD e FAPI

1. **Gestão de Segredos e Sessão**:
   * Token pessoal carregado exclusivamente da variável de ambiente `PLUGGY_USER_TOKEN`.
   * URL base definida em `PLUGGY_USER_API_BASE_URL` (`https://my-api.pluggy.ai`).
   * Zero strings brutas ou tokens embutidos no código.
2. **Privacidade (LGPD)**:
   * Números de conta (`7653187303-1`), agências e CPF (`taxNumber`) são sanitizados e mascarados em todos os logs estruturados com OpenTelemetry.
3. **Tratamento de Exceções RFC 7807**:

| Exceção | Condição de Disparo | Status HTTP | ErrorCode |
| :--- | :--- | :---: | :--- |
| `PluggySessionExpiredDomainException` | API retorna HTTP 401 ou 403 por token expirado | **401** | `PLUGGY_SESSION_EXPIRED` |
| `PluggyApiCommunicationDomainException` | Falha de conectividade ou indisponibilidade da API | **502** | `PLUGGY_API_UNAVAILABLE` |
| `PluggyRateLimitDomainException` | API retorna HTTP 429 persistente | **429** | `PLUGGY_RATE_LIMIT_EXCEEDED` |

---

## 🏗️ 4. Estrutura de Diretórios e Scaffolding

```
src/Services/PluggyIntegration/
├── FinanceHub.PluggyIntegration.Domain/
│   ├── Constants/
│   │   ├── PluggyConstants.cs
│   │   └── PluggyCategoryMapper.cs
│   ├── Exceptions/
│   │   ├── PluggySessionExpiredDomainException.cs
│   │   ├── PluggyApiCommunicationDomainException.cs
│   │   └── PluggyRateLimitDomainException.cs
│   └── ValueObjects/
│       ├── PluggyAccount.cs
│       └── PluggyTransaction.cs
├── FinanceHub.PluggyIntegration.Application/
│   ├── Commands/
│   │   ├── SyncAllPluggyAccounts/
│   │   │   ├── SyncAllPluggyAccountsCommand.cs
│   │   │   ├── ISyncAllPluggyAccountsCommandHandler.cs
│   │   │   └── SyncAllPluggyAccountsCommandHandler.cs
│   ├── DTOs/
│   │   ├── PluggyItemDto.cs
│   │   ├── PluggyAccountDto.cs
│   │   ├── PluggyTransactionDto.cs
│   │   └── SyncPluggySummaryDto.cs
│   ├── Interfaces/
│   │   └── IMeuPluggyClient.cs
│   └── DependencyInjection.cs
├── FinanceHub.PluggyIntegration.Infrastructure/
│   ├── Clients/
│   │   └── MeuPluggyClient.cs
│   ├── Configuration/
│   │   └── PluggyOptions.cs
│   └── DependencyInjection.cs
└── FinanceHub.PluggyIntegration.Api/
    ├── Endpoints/
    │   └── PluggyEndpoints.cs             <-- POST /api/v1/pluggy/sync
    ├── Program.cs
    └── DependencyInjection.cs
```

---

## 🧪 5. Estratégia de Testes (TDD Red-Green-Refactor)

1. **Testes de Cliente HTTP (`MeuPluggyClientTests.cs`)**:
   * Simula respostas de `items`, `accounts` e `transactions` com `FakeHttpMessageHandler`.
   * Verifica se `PluggySessionExpiredDomainException` é disparada em HTTP 401.
2. **Testes de Mapeamento de Categoria (`PluggyCategoryMapperTests.cs`)**:
   * Mapeamento de `Transfer - PIX` $\rightarrow$ `Transferências`.
   * Mapeamento de `Eating out` $\rightarrow$ `Alimentação`.
   * Mapeamento de categoria desconhecida $\rightarrow$ `Outros`.
3. **Testes do Use-Case (`SyncAllPluggyAccountsCommandHandlerTests.cs`)**:
   * Garante que contas `CHECKING_ACCOUNT` publicam `TransactionIngested`.
   * Garante que contas `CREDIT_CARD` publicam `InvoiceItemIngested`.
   * Validação de idempotência e totalizador `SyncPluggySummaryDto`.
4. **Meta de Cobertura**: 80%+ com xUnit, FluentAssertions e NSubstitute.
