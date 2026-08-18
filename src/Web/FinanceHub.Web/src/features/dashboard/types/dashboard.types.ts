export interface AccountBalanceDto {
  accountNumber: string;
  institutionName: string;
  balanceBrl: number;
  badge?: string;
}

export interface DashboardSummaryDto {
  userId: string;
  totalBalanceBrl: number;
  monthlyIncomeBrl: number;
  monthlyExpenseBrl: number;
  accountBalances: AccountBalanceDto[];
  categoryExpenses: {
    categoryName: string;
    amountBrl: number;
    color: string;
  }[];
}
