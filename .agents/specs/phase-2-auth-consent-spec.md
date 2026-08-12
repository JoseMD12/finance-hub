# Phase 2 — AuthConsent Service (`FinanceHub.AuthConsent`) Execution Specification

> **Status**: `Draft / Ready for Implementation`  
> **Target Microservice**: `src/Services/AuthConsent/` (`FinanceHub.AuthConsent.*`)  
> **Database**: PostgreSQL `financehub_authconsent`  
> **Architecture**: Clean Architecture + DDD + Design Patterns  

---

## 🎯 Executive Overview

O microsserviço **`FinanceHub.AuthConsent`** é o gestor centralizado de consentimento OAuth2 e FAPI (Financial-Grade API) para todas as integrações bancárias do FinanceHub (Itaú, Mercado Pago e futuras integrações).

Ele é responsável por:
1. **Gerenciamento do Ciclo de Vida do Consentimento**: Registrar, autorizar, renovar e revogar consentimentos Open Finance.
2. **Renovação Proativa Automática de Tokens (`access_token` / `refresh_token`)**: Um `BackgroundService` em segundo plano com `PeriodicTimer` monitora tokens prestes a expirar (< 5 minutos) e realiza a renovação automática via OAuth2 `grant_type=refresh_token`.
3. **Emissão de Eventos de Integração**: Quando um consentimento é vinculado com sucesso, publica o evento `BankAccountLinked` via MassTransit Outbox.
4. **Segurança & Criptografia (LGPD)**: Criptografia dos tokens at-rest com AES-256-GCM.

---

## 🏛️ Design Patterns Aplicados

| Pattern | Aplicação no Microsserviço | Motivação Arquitetural |
|---------|----------------------------|------------------------|
| **Aggregate Root Pattern** | `BankConsent` | Ponto único de entrada e modificação do estado de domínio. O Value Object `ConsentToken` e entidades internas são imutáveis externamente e manipulados estritamente através dos métodos de negócio da raiz `BankConsent`. |
| **Rich Domain Model** | `BankConsent` & `ConsentToken` | Encapsulamento estrito (setters privados, construtores privados/protected). Regras de validação, cálculo de expiração e transições de estado (`Authorize`, `RotateTokens`, `Revoke`) residem 100% dentro do Domínio, disparando `ConsentDomainException`. |
| **Strategy Pattern** | `IOAuthBankClientStrategy` + `ItauOAuthStrategy`, `MercadoPagoOAuthStrategy` | Encapsular as diferenças específicas de cada API de autorização bancária mantendo a aplicação desacoplada. |
| **Repository Pattern** | `IBankConsentRepository` / `BankConsentRepository` | Abstrair o acesso a dados e isolar as consultas EF Core do domínio, manipulando exclusivamente a Raiz do Agregado. |
| **Background Worker Pattern** | `TokenRenewalBackgroundService` | Executar periodicamente a checagem e renovação proativa de tokens sem bloquear requisições HTTP. |
| **Factory Pattern** | `BankConsentFactory` / Métodos de fábrica na raiz | Centralizar a instanciação válida garantindo todas as invariantes de negócio. |
| **Transactional Outbox Pattern** | `FinanceHub.Shared.Messaging` | Publicar eventos `BankAccountLinked` em transação atômica com o PostgreSQL. |

---

## 📂 Mapeamento Detalhado de Arquivos

### 1. `src/Services/AuthConsent/FinanceHub.AuthConsent.Domain/` (Camada de Domínio Rica + Agregado)
* **Criar**:
  - `Entities/BankConsent.cs`: Raiz do Agregado (**Aggregate Root**) rica, com setters privados, validações de invariantes, métodos expressivos (`RequestConsent(...)`, `Authorize(...)`, `RotateTokens(...)`, `Revoke()`, `IsExpiringSoon()`) e registro de eventos de domínio.
  - `Entities/ConsentStatus.cs`: Enum (`Pending = 1`, `Authorized = 2`, `Revoked = 3`, `Expired = 4`).
  - `ValueObjects/ConsentToken.cs`: **Value Object** imutável contendo `AccessToken`, `RefreshToken`, `ExpiresAtUtc` e `TokenType`, validado em seu construtor privado/fábrica.
  - `Events/ConsentAuthorizedDomainEvent.cs`: Evento de domínio emitido ao autorizar consentimento.
  - `Exceptions/ConsentDomainException.cs`: Exceção de domínio customizada para regras de consentimento.

### 2. `src/Services/AuthConsent/FinanceHub.AuthConsent.Application/` (Camada de Aplicação)
* **Criar**:
  - `Interfaces/IBankConsentRepository.cs`: Interface de contrato de repositório.
  - `Interfaces/IOAuthBankClientStrategy.cs`: Interface Strategy para comunicação OAuth2 com bancos.
  - `DTOs/ConsentResponseDto.cs`, `CreateConsentRequestDto.cs`, `TokenRefreshResponseDto.cs`.
  - `Commands/CreateConsent/CreateConsentCommand.cs` & `CreateConsentCommandHandler.cs`.
  - `Commands/AuthorizeConsent/AuthorizeConsentCommand.cs` & `AuthorizeConsentCommandHandler.cs`.
  - `Commands/RenewToken/RenewTokenCommand.cs` & `RenewTokenCommandHandler.cs`.
  - `Queries/GetConsentByUserId/GetConsentByUserIdQuery.cs` & `GetConsentByUserIdQueryHandler.cs`.

### 3. `src/Services/AuthConsent/FinanceHub.AuthConsent.Infrastructure/` (Camada de Infraestrutura)
* **Criar**:
  - `Persistence/AuthConsentDbContext.cs`: EF Core DbContext isolado para PostgreSQL (`financehub_authconsent`).
  - `Persistence/Configurations/BankConsentConfiguration.cs`: Mapeamento EF Core com conversão de Value Objects e criptografia.
  - `Persistence/Repositories/BankConsentRepository.cs`: Implementação do repositório com EF Core.
  - `Services/OAuthStrategies/ItauOAuthStrategy.cs`: Implementação do Strategy para Itaú Open Finance.
  - `Services/OAuthStrategies/MercadoPagoOAuthStrategy.cs`: Implementação do Strategy para Mercado Pago.
  - `Services/OAuthStrategyFactory.cs`: Factory para selecionar o Strategy correto baseado na instituição (`itau`, `mercadopago`).
  - `BackgroundServices/TokenRenewalBackgroundService.cs`: Worker proativo com `PeriodicTimer` executando a cada 60 segundos.

### 4. `src/Services/AuthConsent/FinanceHub.AuthConsent.Api/` (Camada de API)
* **Alterar**:
  - `Program.cs`: Registrar `AuthConsentDbContext` com Npgsql/PostgreSQL, registradores de DI do MediatR, Strategy Factory, Background Worker e endpoints Minimal API.
* **Criar**:
  - `Endpoints/ConsentEndpoints.cs`: Endpoints REST:
    - `POST /api/v1/consents`: Solicita criação de consentimento.
    - `POST /api/v1/consents/{id}/authorize`: Recebe o código OAuth e gera os tokens iniciais.
    - `GET /api/v1/consents/user/{userId}`: Obtém consentimentos ativos do usuário.
    - `POST /api/v1/consents/{id}/refresh`: Força renovação manual do token.
    - `DELETE /api/v1/consents/{id}`: Revoga consentimento.

### 5. `tests/FinanceHub.UnitTests/AuthConsent/` (Camada de Testes)
* **Criar**:
  - `Domain/BankConsentTests.cs`: Testes unitários puros das regras de transição de estado do consentimento.
  - `Application/AuthorizeConsentCommandHandlerTests.cs`: Testes do Use Case com NSubstitute e Bogus.
  - `Infrastructure/TokenRenewalBackgroundServiceTests.cs`: Testes do Worker de renovação proativa.
  - `Integration/AuthConsentApiIntegrationTests.cs`: Testes E2E via Testcontainers PostgreSQL.

---

## 🗄️ Esquema do Banco de Dados (`financehub_authconsent`)

```sql
CREATE TABLE bank_consents (
    id UUID PRIMARY KEY,
    user_id VARCHAR(100) NOT NULL,
    institution_id VARCHAR(50) NOT NULL,
    consent_token TEXT NOT NULL,
    access_token TEXT NULL,
    refresh_token TEXT NULL,
    token_type VARCHAR(20) DEFAULT 'Bearer',
    expires_at_utc TIMESTAMP WITH TIME ZONE NULL,
    status INT NOT NULL DEFAULT 1,
    created_at_utc TIMESTAMP WITH TIME ZONE NOT NULL,
    updated_at_utc TIMESTAMP WITH TIME ZONE NOT NULL
);

CREATE INDEX idx_bank_consents_user_id ON bank_consents(user_id);
CREATE INDEX idx_bank_consents_status_expires ON bank_consents(status, expires_at_utc);
```

---

## ⚠️ Mapeamento de Exceções de Domínio (RFC 7807 ProblemDetails)

Seguindo a regra de tratamento global ([`.agents/rules/exception-handling-rfc7807.md`](file:///mnt/c/Code/FinanceHub/.agents/rules/exception-handling-rfc7807.md)), todas as falhas de negócio do `FinanceHub.AuthConsent` serão tratadas sem `try/catch` pelo `GlobalExceptionHandler`:

| Exceção de Domínio | Condição de Disparo | Status HTTP | ErrorCode |
|--------------------|---------------------|-------------|-----------|
| `ConsentDomainException` | Dados inválidos ou invariante violada ao criar consentimento | 400 Bad Request | `INVALID_CONSENT_DATA` |
| `ConsentNotFoundException` | Consentimento não localizado no PostgreSQL pelo ID | 404 Not Found | `CONSENT_NOT_FOUND` |
| `ConsentInvalidStateException` | Tentativa de autorizar ou rotacionar consentimento revogado | 409 Conflict | `CONSENT_INVALID_STATE` |
| `UnauthorizedBankException` | Falha na troca de tokens OAuth2 com a API do banco | 401 Unauthorized | `UNAUTHORIZED_BANK_ACCESS` |


---

## 🧪 Plano de Testes & Ciclo TDD (Upfront Test Cases)

Seguindo a regra de **TDD Obrigatório ([`.agents/rules/tdd-workflow.md`](file:///mnt/c/Code/FinanceHub/.agents/rules/tdd-workflow.md))**, toda a codificação da Fase 2 será puxada pela escrita prévia dos testes falhando (**🔴 RED**).

### Bateria 1: Domínio Rico & Agregado (`BankConsent.cs`) — TDD Step 1
- [ ] 🔴 `RequestConsent_ComDadosValidos_DeveCriarConsentimentoEmStatusPending`
- [ ] 🔴 `RequestConsent_ComUserIdVazio_DeveLancarConsentDomainException`
- [ ] 🔴 `Authorize_QuandoPendente_DeveAtualizarTokensEStatusParaAuthorized`
- [ ] 🔴 `Authorize_QuandoJaRevogadoOuExpirado_DeveLancarConsentDomainException`
- [ ] 🔴 `RotateTokens_QuandoTokensValidos_DeveSubstituirConsentTokenEDataExpiracao`
- [ ] 🔴 `Revoke_QuandoAtivo_DeveAlterarStatusParaRevoked`
- [ ] 🔴 `IsExpiringSoon_QuandoFaltarMenosDe5Minutos_DeveRetornarTrue`

### Bateria 2: Use Cases / Application (`AuthorizeConsentCommandHandler.cs`) — TDD Step 2
- [ ] 🔴 `Handle_ComCodigoValido_DeveChamarStrategy_PersistirAgregado_EEMitirBankAccountLinked`
- [ ] 🔴 `Handle_ComConsentimentoInexistente_DeveLancarKeyNotFoundException`

### Bateria 3: Worker de Renovação (`TokenRenewalBackgroundService.cs`) — TDD Step 3
- [ ] 🔴 `ExecuteAsync_QuandoHouverTokensPrestesAExpirar_DeveRenovarProativamente`

### Bateria 4: Testcontainers PostgreSQL Integration — TDD Step 4
- [ ] 🔴 `BankConsentRepository_AddAsync_E_GetByIdAsync_DevePersistirEObterAgregadoNoPostgresIsolado`

