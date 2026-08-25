export interface AccountBalanceDto {
  accountNumber: string;
  institutionId?: string;
  institutionName?: string;
  balanceBrl?: number;
  amount?: number;
  currency?: string;
  lastUpdatedAtUtc?: string;
  badge?: string;
}

export interface DashboardSummaryDto {
  userId: string;
  totalBalanceBrl: number;
  monthlyIncomeBrl?: number;
  monthlyExpenseBrl?: number;
  accountBalances: AccountBalanceDto[];
  categoryExpenses?: {
    categoryName: string;
    amountBrl: number;
    color: string;
  }[];
}
