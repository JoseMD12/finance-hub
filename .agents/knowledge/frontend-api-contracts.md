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

## 💳 2. Endpoint de Transações (`GET /api/v1/transactions`)

### Query Parameters
```typescript
export interface TransactionFiltersDto {
  readonly month?: number; // 1-12
  readonly year?: number; // Ex: 2025
  readonly bank?: string; // 'itau' | 'mercadopago' | 'inter'
  readonly category?: string;
  readonly type?: 'CREDIT' | 'DEBIT' | 'PIX';
  readonly page?: number;
  readonly pageSize?: number;
  readonly search?: string;
}
```

### Resposta TypeScript
```typescript
export interface TransactionDto {
  readonly id: string;
  readonly bankId: string;
  readonly bankName: string;
  readonly amount: number;
  readonly currency: string;
  readonly transactionType: 'INCOME' | 'EXPENSE';
  readonly paymentMethod: 'PIX' | 'DEBIT' | 'CREDIT_SINGLE' | 'CREDIT_INSTALLMENT';
  readonly installmentInfo?: {
    readonly current: number;
    readonly total: number;
  }; // Ex: "2/5"
  readonly description: string;
  readonly category: string;
  readonly transactionDate: string; // ISO 8601
  readonly status: 'CONFIRMED' | 'PENDING';
}

export interface PaginatedTransactionsDto {
  readonly items: readonly TransactionDto[];
  readonly totalCount: number;
  readonly page: number;
  readonly pageSize: number;
  readonly totalPages: number;
}
```

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
