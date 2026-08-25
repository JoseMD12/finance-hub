# Especificação Técnica de Arquitetura: Reconciliação de Saldo Real, Posição de Liquidez e Visões Rápidas de Período

**Documento:** `real-balance-reconciliation-and-view-spec.md`  
**Status:** 🟢 `Concluída & Implementada`  
**Autor:** Antigravity AI & Jose Henrique  
**Branch:** `feature/real-balance-reconciliation-and-filters`  
**Data:** 2026-08-22  

---

## 1. 🎯 Visão Geral e Problema de Negócio

### 1.1 O Desafio
Em plataformas de agregação financeira (PFM) integradas ao Open Finance, usuários frequentemente se deparam com discrepâncias confusas entre:
1. **O Saldo Bancário Real Atual** (a quantia exata disponível hoje somando contas como Itaú, Inter e Mercado Pago);
2. **O Saldo Acumulado do Extrato** (que, ao englobar 13+ meses sem filtro, computa centenas de milhares de reais de fluxo histórico somados);
3. **Volume Fantasma e Movimentações Patrimoniais** (resgates/aportes em caixinhas/cofrinhos, transferências entre contas próprias e pagamentos de fatura de cartão que inflavam artificialmente as receitas e despesas).

### 1.2 A Solução Arquitetural
Estruturar o sistema para fornecer **duas dimensões financeiras complementares e bem delimitadas**:
* **Dimensão 1 — Posição Patrimonial Instantânea (Estoque / Saldo Real)**: O que o usuário tem no banco hoje, quanto tem de fatura a vencer e qual sua liquidez disponível projetada.
* **Dimensão 2 — Fluxo de Caixa do Período (Fluxo Operacional)**: Quanto o usuário recebeu de receitas reais e gastou em despesas de vida dentro de uma janela temporal selecionada (sendo o **Mês Atual** o padrão inteligente).

---

## 2. 🧮 Modelagem Matemática e Contratos de Dados

### 2.1 Posição Patrimonial e Liquidez Instantânea (Snapshot do Open Finance)
$$\text{Saldo Consolidado em Contas} (S_{real}) = \sum_{a \in \text{Contas Correntes}} \text{Balance}(a) \quad [\text{Ex: R\$ 647,48}]$$
$$\text{Passivo em Cartões de Crédito} (F_{aberto}) = \sum_{c \in \text{Cartões}} |\text{Balance}(c)| \quad [\text{Ex: R\$ 3.273,63}]$$
$$\text{Liquidez Livre Projetada} (L_{proj}) = S_{real} - F_{aberto} \quad [\text{Ex: - R\$ 2.626,15}]$$

### 2.2 Fluxo Operacional Filtrado por Janela Temporal $[T_{start}, T_{end}]$
$$\text{Entradas Operacionais} (E) = \sum_{\substack{t \in [T_{start}, T_{end}] \\ t.\text{Type}=\text{Credit} \\ \neg t.\text{IsIgnoredInTotals}}} t.\text{Amount} \quad [\text{Ex: + R\$ 8.793,13 em Agosto/2026}]$$

$$\text{Saídas Operacionais} (S) = \sum_{\substack{t \in [T_{start}, T_{end}] \\ t.\text{Type}=\text{Debit} \\ \neg t.\text{IsIgnoredInTotals}}} t.\text{Amount} \quad [\text{Ex: - R\$ 8.938,30 em Agosto/2026}]$$

$$\text{Resultado do Mês / Economia Líquida} (R) = E - S \quad [\text{Ex: - R\$ 145,17 em Agosto/2026}]$$

---

## 3. 🏛️ Mapeamento Detalhado por Camadas de Arquitetura

```
┌──────────────────────────────────────────────────────────────────────────┐
│                      Web / Frontend (React 19)                           │
│  - TransactionsSummaryCards (4 Cards: Saldo Real, Entradas, Saídas, Res.)│
│  - TransactionsFilterBar (Presets: Mês Atual [Default], Mês Ant., etc.)   │
│  - TransactionsTable (Badges: Transferência, Fatura, Neutro)            │
└──────────────────────────────────┬───────────────────────────────────────┘
                                   │ HTTP (Axios / TanStack Query)
┌──────────────────────────────────▼───────────────────────────────────────┐
│                    BFF Entrypoint (ApiGateway)                           │
│  - GET /api/v1/gateway/transactions (JWT Auth, Claims Extraction)        │
│  - GatewayTransactionSummaryDto (Liquidez Real + Métricas do Período)    │
└──────────────────────────────────┬───────────────────────────────────────┘
                                   │ HTTP Downstream Resiliente (Polly)
┌──────────────────────────────────▼───────────────────────────────────────┐
│              Core Service (FinanceHub.TransactionAggregator)              │
│                                                                          │
│  [Application Layer]                                                     │
│   ├── IGetTransactionsQueryHandler / GetTransactionsQueryHandler         │
│   ├── ITransferPairMatchingEngine / TransferPairMatchingEngine           │
│   └── DTOs: TransactionSummaryDto, PagedTransactionsResponseDto          │
│                                                                          │
│  [Domain Layer]                                                          │
│   ├── CanonicalTransaction (Aggregate Root: Nature, IsIgnoredInTotals)   │
│   ├── AccountBalance (Entity: CurrentBalance, LastUpdatedAtUtc)          │
│   └── Value Objects: Money, AccountIdentifier, TransactionHash           │
│                                                                          │
│  [Infrastructure Layer]                                                  │
│   ├── TransactionRepository (EF Core 10 / Npgsql)                        │
│   │   ├── QueryPagedByFilterAsync (Agregação Operacional + Snapshot)     │
│   │   └── GetUnpairedTransfersCandidateAsync / UpdateRangeAsync          │
│   └── Datasets: merchants.brazil.json & categories.default.json          │
└──────────────────────────────────────────────────────────────────────────┘
```

### 3.1 Camada de Domínio (`Domain`)
* **`CanonicalTransaction`**:
  * `Nature: TransactionNature` (`Operating`, `Transfer`, `BillPayment`, `Investment`, `Adjustment`).
  * `IsIgnoredInTotals: bool` (indica se o lançamento é expurgado dos somatórios de receitas e despesas operacionais).
  * `PairedTransactionId: Guid?` (chave de pareamento entre débito e crédito simétricos de mesma titularidade).
* **`AccountBalance`**:
  * `CurrentBalance: Money` (saldo contábil instantâneo da conta/cartão).
  * `LastUpdatedAtUtc: DateTime` (carimbo de data/hora da sincronização Open Finance).

### 3.2 Camada de Aplicação (`Application`)
* **`TransactionSummaryDto` (Contrato Enriquecido de Resumo)**:
  ```csharp
  public record TransactionSummaryDto(
      decimal TotalIncome,                  // Total de Entradas do período filtrado
      decimal TotalExpense,                 // Total de Saídas do período filtrado
      decimal NetBalance,                   // Resultado / Economia Líquida do período (TotalIncome - TotalExpense)
      int TotalCount,                       // Total de transações contabilizadas
      decimal RealConsolidatedBalanceBrl,   // Saldo real consolidado em contas correntes hoje (R$ 647,48)
      decimal TotalOpenCreditCardsBrl,      // Faturas de cartão em aberto no momento (R$ 3.273,63)
      decimal ProjectedAvailableBalanceBrl, // Liquidez livre projetada (RealConsolidatedBalance - TotalOpenCreditCards)
      DateTime? LastSyncAtUtc               // Timestamp do último snapshot do Open Finance
  );
  ```
* **`ITransferPairMatchingEngine`**:
  * Motor idempotente que reconcilia débitos e créditos de mesmo valor entre contas distintas do mesmo usuário em janela temporal de $\pm 96\text{h}$.

### 3.3 Camada de Infraestrutura (`Infrastructure`)
* **`TransactionRepository.QueryPagedByFilterAsync`**:
  * Executa a consulta de paginação e o somatório `rawTotals` aplicando estritamente `Where(t => !t.IsIgnoredInTotals)`.
  * Consulta os registros de `AccountBalances` para o `UserId` solicitado e computa a posição instantânea de liquidez consolidada (`realConsolidatedBalance`, `openCreditCards`, `projectedAvailable`, `lastSync`).
  * Garante que ambas as dimensões sejam calculadas e retornadas em uma única transação de leitura eficiente.

### 3.4 Camada de BFF (`ApiGateway`)
* **`TransactionGatewayEndpoints.cs`**:
  * Rota: `GET /api/v1/gateway/transactions`
  * Valida o token JWT, extrai o `userId` via Claims e repassa o filtro para o `ITransactionAggregatorServiceClient`.
  * Retorna o `PagedGatewayTransactionsDto` contendo os itens paginados e o `GatewayTransactionSummaryDto` enriquecido.

### 3.5 Camada de Frontend (`Web / FinanceHub.Web`)
* **Contrato TypeScript (`transactions.types.ts`)**:
  ```typescript
  export interface TransactionSummaryDto {
    readonly totalIncome: number;
    readonly totalExpense: number;
    readonly netBalance: number;
    readonly totalCount: number;
    readonly realConsolidatedBalanceBrl?: number;
    readonly totalOpenCreditCardsBrl?: number;
    readonly projectedAvailableBalanceBrl?: number;
    readonly lastSyncAtUtc?: string | null;
  }
  ```
* **Componente `TransactionsSummaryCards.tsx` (4 Cards com Microinterações)**:
  1. **Card 1 (Saldo em Conta Hoje)**:
     - Valor principal destacado: `formatCurrencyBRL(summary.realConsolidatedBalanceBrl)` (Ex: `R$ 647,48`).
     - Badge/rodapé informativo: `Faturas: formatCurrencyBRL(summary.totalOpenCreditCardsBrl)` (Ex: `R$ 3.273,63`) e `Liquidez: formatCurrencyBRL(summary.projectedAvailableBalanceBrl)` (`- R$ 2.626,15`).
     - Subtítulo com status: "Sincronizado via Open Finance".
  2. **Card 2 (Total de Entradas do Período)**:
     - Valor: `+ formatCurrencyBRL(summary.totalIncome)` (Verde).
  3. **Card 3 (Total de Saídas do Período)**:
     - Valor: `- formatCurrencyBRL(summary.totalExpense)` (Vermelho).
  4. **Card 4 (Resultado do Período / Economia Líquida)**:
     - Valor: `formatCurrencyBRL(summary.netBalance)` (Verde se $\ge 0$, Rosa/Vermelho se $< 0$).
     - Subtítulo: "Superávit do período" ou "Déficit operacional".
* **Componente `TransactionsFilterBar.tsx` (Barra de Presets Rápidos)**:
  - Botões de Preset: `[Mês Atual]` *(Selecionado por padrão)* | `[Mês Anterior]` | `[Últimos 30 Dias]` | `[Ano Atual]` | `[Todo o Histórico]`.
  - Ao clicar em um preset, atualiza as datas `startDate` e `endDate` de forma transparente.
* **Componente `TransactionsTable.tsx` (Identificação Visual de Neutralidade)**:
  - Badges nos lançamentos: `Transferência` (ícone `ArrowLeftRight`), `Fatura` (ícone `Receipt`), `Neutro`.
  - Estilo atenuado (`opacity-75 bg-slate-50/40`) para indicar claramente que o item não inflaciona o fluxo operacional.

---

## 4. 📋 Plano de Verificação e Testes

1. **Testes Unitários Backend**:
   - `TransactionRepositoryTests` / `GetTransactionsQueryHandlerTests`: validação dos cálculos de `RealConsolidatedBalanceBrl`, `TotalOpenCreditCardsBrl` e `NetBalance`.
   - `TransferPairMatchingEngineTests`: validação do pareamento em janela de 96h.
2. **Testes de Integração / API**:
   - Validação com `curl` no endpoint do BFF (`/api/v1/gateway/transactions`) e no Aggregator (`/api/v1/transactions`).
3. **Testes Frontend**:
   - `TransactionsPage.test.tsx` e `TransactionsSummaryCards.test.tsx` validando renderização dos 4 cards e troca de presets rápidos.
