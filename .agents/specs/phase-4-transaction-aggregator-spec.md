# Phase 4 — Transaction Aggregator Service Specification (`FinanceHub.TransactionAggregator`)

> **Status**: `Approved & Finalized`  
> **Last Updated**: `2026-08-12`  
> **Author**: `FinanceHub Architecture Team & User`  
> **Audit Score**: `100% DDD & Technical Compliance`

---

## 🎯 Scope & Context

O microsserviço **`FinanceHub.TransactionAggregator`** é a fonte da verdade para o livro razão canônico (Canonical Ledger) e histórico consolidado de transações financeiras.

### Responsabilidades Primárias:
1. **Consumo Assíncrono de Eventos**: Consumir eventos `TransactionIngested` publicados via Transactional Outbox pelos conectores bancários (`ItauIntegration`, `MercadoPagoIntegration`, `InterIntegration`).
2. **Deduplicação Determinística Idempotente**: Garantir matematicamente que transações idênticas trazidas em extratos repetidos não sejam duplicadas no banco de dados.
3. **Livro Razão Canônico (Canonical Ledger)**: Armazenar transações padronizadas com precisão monetária via Value Object `Money` (`decimal(18,2)`), moeda ISO (`BRL`), tipo (`Credit`/`Debit`) e categoria.
4. **Motor Híbrido de Categorização**: Classificação de lançamentos por aprendizado continuado do usuário + sanitizador de nomes de estabelecimentos (`SanitizedDescription`) + regras globais.
5. **Gestão de Saldo Materializado com Concorrência Otimista**: Manter o saldo consolidado das contas atualizado em tempo real no Dashboard (`account_balances`) prevenindo *Lost Updates*.
6. **Consultas CQRS**: Disponibilizar saldo consolidado por usuário (`GetConsolidatedBalanceQuery`), extrato paginado com filtros (`GetTransactionsQuery`) e categorização manual (`CategorizeTransactionCommand`).

> 📌 **Nota de Escopo (IRPF Stand-By)**: A funcionalidade de relatórios de Imposto de Renda e congelamento de saldos em 31/12 (`account_yearly_snapshots`) foi colocada em **Stand-by** e será implementada dedicadamente na **Fase 8 (Módulo IRPF & Tax Analytics)**.

---

## 🏛️ Decisões Arquiteturais Confirmadas

### 1. Estratégia de Deduplicação & Idempotência (`TransactionHash`)
- **Decisão**: Hash SHA-256 Determinístico composto via Value Object `TransactionHash`:
  `SHA256(InstitutionId + AccountId + TransactionDateUtc + Amount + OriginalDescription)`
- **Persistência**: Coluna `hash` (`varchar(64)`) com **Índice Único Composto** no PostgreSQL.
- **Tratamento de Concorrência**: Inserção idempotente com verificação prévia no repositório e captura tratada de exceção de constraint única (`DbUpdateException`), ignorando duplicatas sem interromper o fluxo de ingestão.

---

### 2. Motor de Categorização Híbrido com Aprendizado do Usuário (`CategoryResolverPipeline`)
- **Decisão**: Pipeline de Resolutores (`Chain of Responsibility`) com prioridade hierárquica e sanitização prévia de string:
  1. **Sanitizador de Texto (`TransactionDescriptionSanitizer`)**: Limpa prefixos bancários (`PAG*`, `DB*`, `SAO PAULO BR`, códigos numéricos de terminal) via Value Object `SanitizedDescription`.
  2. **Prioridade 1 — Aprendizado do Usuário (`UserCustomRuleCategoryResolver`)**: Consulta se o usuário já classificou um estabelecimento similar no passado (tabela `user_category_rules`). Se sim, aplica a categoria definida pelo usuário.
  3. **Prioridade 2 — Regras Globais do Sistema (`GlobalPatternCategoryResolver`)**: Avalia padrões globais de palavras-chave/regex (ex: `UBER` $\rightarrow$ `Transporte`, `IFOOD` $\rightarrow$ `Alimentação`).
  4. **Prioridade 3 — Fallback Padrão (`DefaultFallbackCategoryResolver`)**: Atribui a categoria padrão `"Outros"` / `"Não Categorizado"`.
- **Precedência do Usuário (`IsManuallyCategorized`)**:
  - Quando o usuário altera manualmente a categoria via endpoint `PUT /api/v1/transactions/{id}/category`, o sistema salva a transação com `IsManuallyCategorized = true` e cria/atualiza a regra em `user_category_rules`.
  - Transações com `IsManuallyCategorized = true` NUNCA são sobrescritas em reprocessamentos automáticos.

---

### 3. Gestão de Saldos com Concorrência Otimista (`AccountBalance`)
- **Decisão**: Saldo Materializado na tabela `account_balances` protegido por **Optimistic Concurrency Control**.
- **Funcionamento & Proteção contra Lost Updates**:
  - Tabela `account_balances` (`user_id`, `institution_id`, `account_id`, `current_balance`, `currency`, `last_updated_at_utc`, `xmin`).
  - Utiliza coluna de concorrência `xmin` no PostgreSQL (`builder.Property<uint>("xmin").IsRowVersion()`) para evitar sobrescritas concorrentes quando eventos chegam simultaneamente.
  - Consulta de saldo total consolidado do Dashboard realizada em `< 1ms` (`SELECT SUM(current_balance) FROM account_balances WHERE user_id = @userId`).

---

### 4. Endpoints REST & CQRS Use Cases

| Método | Endpoint | Interface de Handler | DTO de Resposta / Payloads |
|---|---|---|---|
| **`GET`** | `/api/v1/balances/consolidated/{userId}` | `IGetConsolidatedBalanceQueryHandler` | `ConsolidatedBalanceDto` (saldo total + quebra por instituição) |
| **`GET`** | `/api/v1/transactions` | `IGetTransactionsQueryHandler` | `PagedResult<CanonicalTransactionDto>` (filtros: `userId`, `startDate`, `endDate`, `institutionId`, `categoryId`, `page`, `pageSize`) |
| **`PUT`** | `/api/v1/transactions/{id}/category` | `ICategorizeTransactionCommandHandler` | Body: `{"categoryId": "guid"}` $\rightarrow$ Retorna `200 OK` e atualiza `user_category_rules` |

---

### 5. Modelo de Domínio Rich DDD (Value Objects & Owned Entities)

#### 🧱 Value Objects Imutáveis (`Domain/ValueObjects/` - C# 13 Record Types):
1. **`Money`** (`record Money(decimal Amount, string Currency)`):
   - Validação de precisão de 2 casas decimais e verificação estrita de moeda em operações (`Add`, `Subtract`). Lança `CurrencyMismatchDomainException` se as moedas divergirem.
2. **`TransactionHash`** (`record TransactionHash(string Value)`):
   - Valida no construtor formato Hexadecimal SHA-256 imutável de 64 caracteres.
3. **`SanitizedDescription`** (`record SanitizedDescription(string OriginalText, string CleanText)`):
   - Executa limpeza prévia de prefixos bancários.
4. **`AccountIdentifier`** (`record AccountIdentifier(string InstitutionId, string AccountId)`):
   - Agrupa identificadores de conta com igualdade por valor.
5. **`TransactionAuditInfo`** (`record TransactionAuditInfo(DateTime CreatedAtUtc, DateTime UpdatedAtUtc)`):
   - Metadados imutáveis de auditoria temporais.

#### 🚨 Hierarquia de Exceções de Domínio (`Domain/Exceptions/`):
- `TransactionAggregatorDomainException` (base derivada de `DomainException`)
- `CurrencyMismatchDomainException` ("Não é possível realizar operações financeiras entre moedas distintas.")
- `InvalidMoneyAmountDomainException` ("Valor monetário inválido.")
- `InvalidTransactionHashDomainException` ("Hash SHA-256 da transação deve ter exatamente 64 caracteres hexadecimais.")
- `CanonicalTransactionNotFoundDomainException` ("Transação canônica não encontrada.")
- `InvalidCategoryIdDomainException` ("Identificador de categoria inválido.")

#### 📦 Owned Entities & Aggregate Root (`Domain/Entities/`):
1. **`CanonicalTransaction` (Aggregate Root)**:
   - Contém Value Objects (`AccountInfo`, `Hash`, `Amount`, `Description`, `AuditInfo`), Enum (`Type`, `CategorizationSource`), `CategoryId`, `IsManuallyCategorized`, `TransactionDateUtc`.
   - Contém Owned Entity **`BankTransactionDetails`** (`BankTransactionId`, `Channel`, `MerchantName`).
   - Tabela: `canonical_transactions` com `idx_canonical_transactions_hash` (único) e `idx_canonical_transactions_user_date`.

2. **`AccountBalance` (Aggregate Root)**:
   - `Id` (`Guid`), `UserId` (`string`), `AccountInfo` (`AccountIdentifier`), `CurrentBalance` (`Money`), `LastUpdatedAtUtc` (`DateTime`), Token de Concorrência `xmin`.
   - Tabela: `account_balances` com `idx_account_balances_user_inst_acc` (único).

3. **`UserCategoryRule` (Aggregate Root / Entity)**:
   - `Id` (`Guid`), `UserId` (`string`), `Pattern` (`string`), `CategoryId` (`Guid`), `CreatedAtUtc` (`DateTime`).
   - Tabela: `user_category_rules` com `idx_user_category_rules_user_pattern` (único).

---

### 6. Estratégia de Testes Unitários & Integração (TDD Mandatory Workflow)

- **Workflow Mandatório**: Todo Use Case, Value Object e Entidade deve iniciar com um teste falhando (**Red**), passar com o código mínimo (**Green**) e ser refatorado (**Refactor**).
- **Cobertura Mínima**: **80% de cobertura** no microsserviço `FinanceHub.TransactionAggregator`.
- **Cenários Cobertos**:
  - `MoneyValueObjectTests`: Testes de imutabilidade, operações matemáticas e lançamento de `CurrencyMismatchDomainException`.
  - `TransactionHashValueObjectTests`: Validação estrita de hash Hex 64 caracteres.
  - `CanonicalTransactionTests`: Criação, validação de hash SHA-256, alteração de categoria com flag `IsManuallyCategorized`.
  - `CategoryResolverPipelineTests`: Sanitização de string, resolução por regra de usuário, resolução por regra global e fallback.
  - `IngestTransactionCommandHandlerTests`: Ingestão idempotente ignorando duplicatas.
  - `GetConsolidatedBalanceQueryHandlerTests`: Agregação correta dos saldos com controle de concorrência.
