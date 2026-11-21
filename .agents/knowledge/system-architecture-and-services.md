# 🏛️ FinanceHub — Arquitetura de Sistemas, Microsserviços e Topologia de Dados

> **Documento de Referência Arquitetural e Decisão de Engenharia (ADR / Knowledge Base)**  
> **Última Atualização:** 2026-11-21  
> **Status:** Ativo / Consolidado

---

## 🎯 1. Visão Geral & Evolução Arquitetural

O **FinanceHub** é uma plataforma de agregação, controle e inteligência financeira pessoal desenvolvida em **.NET 10** e **C# 13**, estruturada sob **Clean Architecture** e **Domain-Driven Design (DDD)**.

### ADR-001 — Acesso ao Open Finance via Meu.Pluggy, sem conectores diretos por banco

**Data:** 16–17/08/2026 · **Status:** Aceita · **Implementada em:** PRs #13 e #14

#### Contexto

O plano original previa um conector próprio por instituição (`ItauIntegration`, `InterIntegration`,
`MercadoPagoIntegration`), falando FAPI 1.0/2.0 diretamente com cada banco: `private_key_jwt`,
PAR, PKCE, DPoP e mTLS com certificado ICP-Brasil, além do serviço `AuthConsent` para o ciclo de
vida do consentimento e da biblioteca `FinanceHub.Shared.Certificates`.

#### Decisão

Rejeitar os conectores diretos e obter os dados pelo **Meu.Pluggy**, o agregador Open Finance
gratuito para pessoa física. O `FinanceHub.PluggyIntegration` passa a ser o único ponto de
entrada de dados bancários online.

#### Motivos

1. **Custo e burocracia.** Falar FAPI direto com os bancos exige certificado ICP-Brasil de pessoa
   jurídica e credenciamento no Diretório do Open Finance Brasil. Para um uso pessoal, o custo não
   se paga (plano registrado em `.agents/plans/openfinance_real_access_plan.md`, rejeitado).
2. **As APIs de desenvolvedor dos bancos não atendem leitura de pessoa física.** A do Inter é
   voltada a PJ (Pix e cobrança) e a do Mercado Pago a vendedores/checkout; nenhuma expõe itens de
   fatura de cartão PF (planos de Inter e Mercado Pago, rejeitados).
3. **O Meu.Pluggy resolve a ingestão sem intermediário pago**, cobrindo Itaú, Inter e Mercado Pago
   numa única integração.

#### Consequências

- **Removidos:** os três conectores, o serviço `AuthConsent` e a biblioteca
  `FinanceHub.Shared.Certificates`.
- **FAPI, mTLS e consentimento passam a ser responsabilidade da Pluggy** com cada instituição. O
  FinanceHub não implementa OAuth/FAPI, não guarda credenciais bancárias nem tokens de
  consentimento, e não gerencia o ciclo de vida do consentimento — o usuário conecta e revoga
  instituições no próprio `meu.pluggy.ai`.
- **O token pessoal da Meu.Pluggy viaja por requisição** no header `X-Pluggy-Access-Token`, via
  Gateway, e não é persistido no backend.
- **As regras de FAPI, mTLS e KMS do harness foram reescritas** para este modelo
  (`AGENTS.md`, Regras 3 a 6, e `.agents/rules/openfinance-security.md`).
- **Nova instituição = conectar no Meu.Pluggy**, não escrever conector. Reintroduzir conector
  direto exige um novo ADR.

#### Custos aceitos

- **Dependência de terceiro e do plano gratuito**, cujas condições podem mudar.
- **Token pessoal com expiração**, obtido pelo usuário (extensão ou colar manual); quando expira,
  a API devolve 403 e o sync para até um token novo ser informado.
- **Fidelidade limitada ao que a Pluggy entrega.** Observado em 27/08/2026: `balanceCloseDate`
  vem nulo, `balanceDueDate` traz o vencimento da fatura já fechada e `billForecastDate` falta em
  cerca de um terço das transações de cartão (ver `dashboard-cycle-expectation-spec.md` §2.2).

### Modelo de serviços resultante

1. **Conexão Primária Online (On-Demand)**: Centralizada no microsserviço **`FinanceHub.PluggyIntegration`**, que consome a API do **Meu.Pluggy** (B2C / Pessoa Física), cobrindo de forma unificada **Itaú**, **Banco Inter** e **Mercado Pago**.
2. **Motor de Importação Offline (Fallback)**: Centralizado no microsserviço **`FinanceHub.FileImporter`**, responsável pelo processamento de arquivos físicos históricos (`.ofx`, `.csv`, `.pdf`) quando a sincronização online não for viável.
3. **Motor Canônico e Contábil**: Centralizado no **`FinanceHub.TransactionAggregator`**, garantindo deduplicação estrita (SHA-256), regras de categorização automática, reconciliação de transferências/faturas e projeções financeiras. É serviço interno: não tem autenticação própria e só é alcançável pelo Gateway (no compose, `expose` em vez de `ports`).

---

## 🏛️ 2. Matriz Consolidada de Microsserviços

```
/
├── src/
│   ├── Services/
│   │   ├── ApiGateway/                  <-- [BFF] Ponto de entrada unificado, auth e roteamento
│   │   ├── PluggyIntegration/          <-- [Online] Conector Open Finance Pessoal (Itaú, Inter, MP)
│   │   ├── FileImporter/               <-- [Offline] Motor de importação de arquivos (OFX, CSV, PDF)
│   │   └── TransactionAggregator/       <-- [Core DDD] Ledger canônico, deduplicação SHA-256 e regras
│   │
│   └── Shared/
│       ├── FinanceHub.Shared.Messaging/     <-- MassTransit / RabbitMQ, Outbox e Contratos de Eventos
│       └── FinanceHub.Shared.Observability/ <-- OpenTelemetry (Tracing/Metrics) e Serilog
└── tests/
    └── FinanceHub.UnitTests/            <-- Testes unitários e de integração com Testcontainers
```

---

## 📦 3. Responsabilidade Detalhada por Microsserviço

### 🌐 3.1 `FinanceHub.ApiGateway` (Porta `5050`)
* **Papel**: Backend For Frontend (BFF) unificado.
* **Responsabilidades**:
  * Autenticação e validação de tokens JWT / Scopes (`openfinance:read`, `openfinance:write`).
  * Rate Limiting centralizado com particionamento por IP e usuário.
  * Roteamento HTTP com resiliência padrão (`AddStandardResilienceHandler`).
  * Agregação de endpoints do Dashboard e transações para o frontend web.

### ⚡ 3.2 `FinanceHub.PluggyIntegration` (Porta `5056`)
* **Papel**: Conector unificado de Open Finance Pessoal via Meu.Pluggy.
* **Responsabilidades**:
  * Orquestração de chamadas à API `https://my-api.pluggy.ai` via `MeuPluggyClient` com resiliência Polly (Retry exponencial + Jitter para 5xx e 429).
  * Tradução de entidades bancárias e enriquecimento de categorias via Camada Anti-Corrupção (`PluggyCategoryMapper`).
  * Publicação assíncrona dos eventos de domínio no barramento RabbitMQ:
    * `TransactionIngested`: Transações de contas correntes / contas pagamento.
    * `InvoiceItemIngested`: Compras e parcelamentos de faturas de cartão de crédito.
  * Tratamento de sessão expirada e erros através de exceções de domínio RFC 7807 (`PluggySessionExpiredDomainException`, `PluggyRateLimitDomainException`, `PluggyApiCommunicationDomainException`).

### 📄 3.3 `FinanceHub.FileImporter` (Porta `5006` - Em Construção)
* **Papel**: Motor de ingestão offline para arquivos de extrato e fatura.
* **Responsabilidades**:
  * Parser de extratos bancários em formato **OFX** (Banco Inter).
  * Parser de extratos bancários em formato **CSV** (Mercado Pago).
  * Parsers de extratos e faturas em formato **PDF** via `PdfPig` (Itaú Extrato/Fatura, Mercado Pago Cartão).
  * Emissão dos mesmos eventos canônicos (`TransactionIngested` / `InvoiceItemIngested`) para o barramento.

### 🧠 3.4 `FinanceHub.TransactionAggregator` (Porta `5002`, interna à rede do compose)
* **Papel**: Núcleo contábil, deduplicação e consolidação de patrimônio.
* **Responsabilidades**:
  * Consumo de eventos via MassTransit com **Transactional Outbox** em PostgreSQL.
  * **Deduplicação por Hash SHA-256 (`TransactionHash.ComputeHash`)**: Impede a inserção de transações repetidas independente da fonte (API ou arquivo).
  * **Pipeline de Categorização Automática**: Regras do Usuário $\rightarrow$ Regras Globais de Estabelecimentos $\rightarrow$ Fallback.
  * **Motor de Reconciliação Financeira**: Neutralização contábil de pagamentos de fatura de cartão e transferências entre contas próprias (Anti-Double Counting).
  * Armazenamento exclusivo no banco PostgreSQL `financehub_transactionaggregator`.

---

## 🔄 4. Topologia de Mensageria e Eventos de Domínio

```mermaid
flowchart LR
    Pluggy[FinanceHub.PluggyIntegration] -->|TransactionIngested| RMQ[(RabbitMQ)]
    Pluggy -->|InvoiceItemIngested| RMQ
    FileImp[FinanceHub.FileImporter] -->|TransactionIngested| RMQ
    FileImp -->|InvoiceItemIngested| RMQ

    RMQ -->|transaction-ingested| Agg[FinanceHub.TransactionAggregator]
    RMQ -->|invoice-item-ingested| Agg

    Agg -->|TransactionNormalized| Outbox[(PostgreSQL Outbox)]
```

### Contratos de Eventos (`FinanceHub.Shared.Messaging`):
1. **`TransactionIngested`**:
   * `IngestionId`, `Source`, `AccountId`, `BankTransactionId`, `Amount`, `TransactionDate`, `Description`, `Currency`, `RawPayloadJson`, `OccurredAtUtc`.
2. **`InvoiceItemIngested`**:
   * `IngestionId`, `Source`, `CreditCardAccountId`, `CardLastFourDigits`, `BankTransactionId`, `Amount`, `TransactionDate`, `Description`, `Category`, `CurrentInstallment`, `TotalInstallments`, `InvoiceDueDate`, `Currency`, `RawPayloadJson`, `OccurredAtUtc`.
3. **`AccountBalanceSnapshotSynchronized`**:
   * `UserId`, `InstitutionId`, `AccountId`, `BalanceBrl`, `Currency`, `SynchronizedAtUtc`.

---

## 🔒 5. Segurança, LGPD e Guardrails Arquiteturais

1. **Isolamento Estrito de Banco de Dados**: Cada serviço acessa exclusivamente seu próprio banco PostgreSQL. Comunicação entre serviços ocorre exclusivamente via HTTP BFF ou Mensageria RabbitMQ.
2. **Zero Segredos em Código**: Tokens e credenciais bancárias são carregados dinamicamente via `.env` / variáveis de ambiente.
3. **Privacidade e LGPD**: Sanitização mandatória de logs (nenhum CPF, número de cartão completo ou dado sensível é logado).
4. **Resiliência e Idempotência**: Todo fluxo de ingestão suporta reexecução on-demand sem gerar incongruências ou duplicidades contábeis.
