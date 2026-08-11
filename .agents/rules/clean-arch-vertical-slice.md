# Clean Architecture + Microservices Rules — FinanceHub (.NET 10)

## 1. Microservices Ecosystem & Boundaries
FinanceHub is structured into autonomous microservices (`src/Services/`) and shared modules (`src/Shared/`).

### Service Boundaries & Rules
1. **`FinanceHub.AuthConsent`**: OAuth2/OIDC + FAPI flow per bank institution. Manages consent, tokens (`access_token`, `refresh_token`). Exposes internal API for valid tokens.
2. **`FinanceHub.ItauIntegration`**: Consumes Itaú Open Finance API. Translates response to `TransactionIngested` event (`source: "Itau"`). No direct link to Mercado Pago or Inter.
3. **`FinanceHub.MercadoPagoIntegration`**: Same as Itaú, isolated for Mercado Pago.
4. **`FinanceHub.InterIntegration`**: Banco Inter API connector (to implement after Inter phase confirmation).
5. **`FinanceHub.TransactionAggregator`**: Consumes `TransactionIngested` from any bank, normalizes to canonical model, deduplicates, and persists history. Emits `TransactionNormalized`.
6. **`FinanceHub.ApiGateway`**: Single entrypoint BFF for frontend/app. No direct DB access — calls services via internal API.

## 2. Microservices Strict Constraints
- **Database Isolation**: Each service owns its database. Direct database calls to another service's DB are STRICTLY PROHIBITED.
- **Async Communication**: Inter-service events use RabbitMQ / MassTransit with the Transactional Outbox Pattern.
- **No Integration Coupling**: Never reference one Bank Integration Service from another.
- **Shared Libraries**: Use `FinanceHub.Shared.Certificates`, `FinanceHub.Shared.Messaging`, and `FinanceHub.Shared.Observability`. No business logic allowed in `Shared.*`.

## 3. Clean Architecture within Services
Each service follows Clean Architecture:
- `Domain`: Pure aggregates, value objects, domain events. Zero external dependencies.
- `Application`: CQRS Use cases, MediatR handlers, FluentValidation rules.
- `Infrastructure`: EF Core DbContext, Outbox pattern, mTLS clients.
- `Api`: Minimal API endpoints (.NET 10).
