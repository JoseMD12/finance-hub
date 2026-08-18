/**
 * DTOs and types for Open Finance Connections and Meu.Pluggy synchronization.
 */

export interface PluggySyncSummaryDto {
  totalItemsSynced: number;
  totalAccountsSynced: number;
  totalCheckingTransactionsIngested: number;
  totalCardTransactionsIngested: number;
  syncedAtUtc: string;
}

export interface ConnectedAccountDto {
  accountNumber: string;
  institutionName: string;
  balanceBrl: number;
  badge?: string;
  lastUpdatedAtUtc?: string;
}

export interface PluggySessionState {
  token: string;
  hasToken: boolean;
  lastSync?: PluggySyncSummaryDto | null;
}
