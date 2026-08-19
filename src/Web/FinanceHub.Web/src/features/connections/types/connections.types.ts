export interface PluggyConnectorDto {
  id: number;
  name: string;
}

export interface PluggyItemDto {
  id: string;
  status: string;
  connector: PluggyConnectorDto;
  totalBalance: number;
  accountsCount: number;
  totalCredit: number;
  lastUpdatedAt?: string | null;
}

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
