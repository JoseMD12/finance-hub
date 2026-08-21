export interface CategoryDto {
  readonly id: string;
  readonly name: string;
  readonly slug: string;
  readonly parentCategoryId?: string | null;
  readonly iconKey: string;
  readonly colorToken: string;
  readonly isSystemDefault: boolean;
  readonly isActive: boolean;
  readonly subcategories?: CategoryDto[];
}

export interface TransactionDto {
  readonly id: string;
  readonly userId: string;
  readonly institutionId: string;
  readonly accountNumber: string;
  readonly amount: number;
  readonly currency: string;
  readonly type: 'Credit' | 'Debit' | string;
  readonly description: string;
  readonly categoryId: string;
  readonly categorizationSource: string;
  readonly isManuallyCategorized: boolean;
  readonly transactionDateUtc: string;
  readonly channel: string;
  readonly merchantName: string;
}

export interface TransactionSummaryDto {
  readonly totalIncome: number;
  readonly totalExpense: number;
  readonly netBalance: number;
  readonly totalCount: number;
}

export interface PaginatedTransactionsDto {
  readonly items: TransactionDto[];
  readonly summary: TransactionSummaryDto;
  readonly page: number;
  readonly pageSize: number;
  readonly totalItems: number;
  readonly totalPages: number;
}

export interface TransactionFilterParams {
  readonly page?: number;
  readonly pageSize?: number;
  readonly startDate?: string;
  readonly endDate?: string;
  readonly datePreset?: number;
  readonly institutionId?: string;
  readonly categoryId?: string;
  readonly type?: string;
  readonly search?: string;
}

export interface CategorizeTransactionPayload {
  readonly transactionId: string;
  readonly categoryId: string;
  readonly createCustomRule: boolean;
}
