# FinanceHub — Frontend API Contracts & DTOs

> **BFF Base URL**: `http://localhost:5000` (`FinanceHub.ApiGateway`)  
> **Auth Scheme**: `Authorization: Bearer <jwt_token>`

---

## 📊 1. Endpoint de Dashboard Agregado (`GET /api/v1/gateway/dashboard`)

### Resposta TypeScript
```typescript
export interface BankAccountBalanceDto {
  readonly bankId: string; // 'itau' | 'mercadopago' | 'inter'
  readonly bankName: string; // 'Itaú Unibanco' | 'Mercado Pago' | 'Banco Inter'
  readonly accountId: string;
  readonly balance: number;
  readonly currency: string; // 'BRL'
  readonly lastSyncAt: string; // ISO 8601
  readonly status: 'ACTIVE' | 'WARNING' | 'ERROR';
}

export interface ExpenseByCategoryDto {
  readonly categoryId: string;
  readonly categoryName: string;
  readonly totalAmount: number;
  readonly percentage: number; // Ex: 45.0
  readonly colorHex?: string;
}

export interface DashboardResponseDto {
  readonly totalConsolidatedBalance: number;
  readonly totalIncomeCurrentMonth: number;
  readonly totalExpenseCurrentMonth: number;
  readonly accounts: readonly BankAccountBalanceDto[];
  readonly categoryExpenses: readonly ExpenseByCategoryDto[];
  readonly recentTransactions: readonly TransactionDto[];
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

## 🏦 3. Endpoint de Consentimentos (`GET /api/v1/consents`)

### Resposta TypeScript
```typescript
export interface BankConsentDto {
  readonly id: string;
  readonly bankId: 'itau' | 'mercadopago' | 'inter';
  readonly bankName: string;
  readonly status: 'AUTHORISED' | 'AWAITING_AUTHORISATION' | 'REJECTED' | 'REVOKED';
  readonly createdAt: string;
  readonly expiresAt: string;
  readonly accountsCount: number;
}

export interface CreateConsentRequestDto {
  readonly bankId: 'itau' | 'mercadopago' | 'inter';
  readonly permissions: readonly string[];
}

export interface CreateConsentResponseDto {
  readonly consentId: string;
  readonly redirectUri: string; // URL do banco para autorização FAPI
}
```
