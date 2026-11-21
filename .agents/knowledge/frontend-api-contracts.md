# FinanceHub — Frontend API Contracts & DTOs

> **BFF Base URL**: `http://localhost:5000` (`FinanceHub.ApiGateway`)  
> **Auth Scheme**: `Authorization: Bearer <jwt_token>`

---

## 📊 1. Endpoint de Dashboard Agregado (`GET /api/v1/gateway/dashboard`)

**Fonte da verdade:** `src/Web/FinanceHub.Web/src/features/dashboard/types/dashboard.types.ts`, que
espelha `GatewayDashboardSummaryDto` (Gateway) e `DashboardSummaryDto` (Aggregator). O `userId` vem
da claim do JWT, nunca da query string.

**Query string (todos opcionais):** `startDate`, `endDate`, `institutionId`, `includeIgnoredInTotals`.
O filtro de período não se aplica à série de evolução mensal (`monthlyCashFlow`, últimos seis
meses); instituição e neutralidade se aplicam a tudo.

### Resposta TypeScript (resumo)
```typescript
export interface DashboardSummaryDto {
  readonly userId: string;
  readonly summary: DashboardSummaryTotalsDto; // KPIs, mesmo cálculo da tela de Transações
  readonly institutionBalances: readonly InstitutionBalanceDto[];
  readonly categoryExpenses: readonly CategoryExpenseDto[]; // top N + cauda "Outras"
  readonly monthlyCashFlow: readonly MonthlyCashFlowPointDto[];
  readonly recentTransactions: readonly TransactionDto[];
  readonly generatedAtUtc: string;
}

export interface InstitutionBalanceDto {
  readonly institutionId: string; // nome e logo resolvidos no cliente por getInstitutionInfo()
  readonly accountNumber: string;
  readonly balanceBrl: number;
  readonly currency: string;
  readonly isCreditCard: boolean;
  readonly creditLimit?: number | null; // limite concedido pelo banco
  readonly availableCreditLimit?: number | null;
  readonly usedCreditLimit?: number | null; // creditLimit - availableCreditLimit
  readonly customizedCreditLimit?: number | null; // limite definido pelo titular; mede o consumo quando existe
  readonly invoiceDueDateUtc?: string | null; // vencimento da última fatura fechada, como a Pluggy envia
  readonly lastUpdatedAtUtc: string;
}
```

---

## 💳 2. Endpoints de Transações (`/api/v1/gateway/transactions`)

### Query Parameters (`TransactionFilterParams`)
```typescript
export interface TransactionFilterParams {
  readonly page?: number;
  readonly pageSize?: number;
  readonly startDate?: string; // ISO 8601
  readonly endDate?: string; // ISO 8601
  readonly datePreset?: string; // 'current-month' | 'previous-month' | 'last-30' | 'current-year' | 'all-time'
  readonly institutionId?: string; // 'itau' | 'inter' | 'mercadopago'
  readonly categoryId?: string;
  readonly type?: string; // 'Debit' | 'Credit'
  readonly search?: string;
  readonly includeIgnoredInTotals?: boolean; // Padrão: false (Filtra neutros, faturas e transferências da listagem e dos totais)
}
```

### Resposta de Listagem (`PaginatedTransactionsDto`)
```typescript
export interface TransactionDto {
  readonly id: string;
  readonly userId: string;
  readonly institutionId: string;
  readonly accountNumber: string;
  readonly amount: number;
  readonly currency: string;
  readonly type: 'Credit' | 'Debit';
  readonly description: string;
  readonly categoryId: string;
  readonly categorizationSource: string;
  readonly isManuallyCategorized: boolean;
  readonly transactionDateUtc: string;
  readonly channel: string;
  readonly merchantName: string;
  readonly nature?: 'Operating' | 'Transfer' | 'BillPayment';
  readonly isBillPayment?: boolean;
  readonly isIgnoredInTotals?: boolean;
  readonly pairedTransactionId?: string | null;
}

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

export interface PaginatedTransactionsDto {
  readonly items: TransactionDto[];
  readonly summary: TransactionSummaryDto;
  readonly page: number;
  readonly pageSize: number;
  readonly totalItems: number;
  readonly totalPages: number;
}
```

### Endpoints de Alteração Parcial (PATCH)
- **Categorização**: `PATCH /api/v1/gateway/transactions/{id}/category`
  - Payload: `{ categoryId: string, createCustomRule: boolean, applyToPastTransactions?: boolean }`
- **Alternar Neutralidade / Trânsito**: `PATCH /api/v1/gateway/transactions/{id}/neutrality`
  - Payload: `{ isIgnoredInTotals: boolean, reason?: string }`
- **Alternar Pagamento de Fatura**: `PATCH /api/v1/gateway/transactions/{id}/bill-payment`
  - Payload: `{ isBillPayment: boolean }`

---

## 🏦 3. Conexões Meu.Pluggy (`/api/v1/gateway/pluggy`)

Não existe endpoint de consentimentos: o consentimento Open Finance é concedido e revogado pelo
usuário no `meu.pluggy.ai` (ADR-001). As rotas que falam com a Pluggy exigem, além do JWT do
FinanceHub, o header `X-Pluggy-Access-Token` com o token pessoal do usuário; sem ele o Gateway
responde RFC 7807. A consulta de status do job não fala com a Pluggy e dispensa o token.

| Rota | Uso |
| --- | --- |
| `GET /items` | Instituições conectadas, com status, saldo total e quantidade de contas (`PluggyItemDto`). |
| `GET /accounts` | Contas correntes e de cartão, com dados de crédito. |
| `POST /items/{itemId}/sync` | Ressincroniza uma única instituição. |
| `POST /sync` | Dispara a sincronização em segundo plano; responde 202 com `jobId` (`SyncJobAcceptedDto`). |
| `GET /sync/jobs/{jobId}` | Status do job, com o resumo ao concluir (`SyncJobStatusDto`). |

**Fonte da verdade:** `src/Web/FinanceHub.Web/src/features/connections/types/connections.types.ts`
e `src/shared/api/apiEndpoints.ts`.

