import type { DatePresetKey } from '@/shared/types/filters.types';
import type { TransactionDto } from '@/features/transactions/types/transactions.types';

/**
 * Contrato real devolvido por `GET /api/v1/gateway/dashboard`.
 *
 * A versão anterior deste arquivo descrevia campos que o Gateway nunca enviou
 * (`monthlyIncomeBrl`, `categoryExpenses`, `institutionName`), o que mantinha a tela renderizando
 * zeros e `undefined`. Estes tipos espelham `GatewayDashboardSummaryDto`.
 */

export interface DashboardFilterParams {
  readonly startDate?: string;
  readonly endDate?: string;
  readonly datePreset?: DatePresetKey;
  readonly institutionId?: string;
  readonly includeIgnoredInTotals?: boolean;
}

/** KPIs do período. Mesmo cálculo que alimenta a tela de Transações. */
export interface DashboardSummaryTotalsDto {
  readonly totalIncome: number;
  readonly totalExpense: number;
  readonly netBalance: number;
  readonly totalCount: number;
  readonly realConsolidatedBalanceBrl?: number;
  readonly totalOpenCreditCardsBrl?: number;
  readonly projectedAvailableBalanceBrl?: number;
  readonly lastSyncAtUtc?: string | null;
}

/**
 * Saldo de uma conta. O nome de exibição e o logotipo são resolvidos no cliente a partir de
 * `institutionId`, via `getInstitutionInfo()` — o backend não duplica catálogo de bancos.
 */
export interface InstitutionBalanceDto {
  readonly institutionId: string;
  readonly accountNumber: string;
  readonly balanceBrl: number;
  readonly currency: string;
  readonly isCreditCard: boolean;
  readonly creditLimit?: number | null;
  readonly availableCreditLimit?: number | null;
  readonly usedCreditLimit?: number | null;
  readonly invoiceDueDateUtc?: string | null;
  readonly lastUpdatedAtUtc: string;
}

export interface CategoryExpenseDto {
  readonly categoryId: string;
  readonly categoryName: string;
  readonly colorToken: string;
  readonly iconKey: string;
  readonly amountBrl: number;
  /** Fatia do total de despesas do período, já calculada no servidor. */
  readonly percentage: number;
}

export interface MonthlyCashFlowPointDto {
  readonly year: number;
  readonly month: number;
  readonly incomeBrl: number;
  readonly expenseBrl: number;
  readonly netBrl: number;
}

export interface DashboardSummaryDto {
  readonly userId: string;
  readonly summary: DashboardSummaryTotalsDto;
  readonly institutionBalances: readonly InstitutionBalanceDto[];
  readonly categoryExpenses: readonly CategoryExpenseDto[];
  readonly monthlyCashFlow: readonly MonthlyCashFlowPointDto[];
  readonly recentTransactions: readonly TransactionDto[];
  readonly generatedAtUtc: string;
}
