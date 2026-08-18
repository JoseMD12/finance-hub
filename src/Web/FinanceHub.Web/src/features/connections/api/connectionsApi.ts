import { httpClient } from '@/shared/api/httpClient';
import { API_ENDPOINTS, API_HEADERS } from '@/shared/api/apiEndpoints';

export interface PluggySyncSummaryDto {
  totalItemsSynced: number;
  totalAccountsSynced: number;
  totalCheckingTransactionsIngested: number;
  totalCardTransactionsIngested: number;
  syncedAtUtc: string;
}

export const syncPluggyAccountsApi = async (pluggyAccessToken: string): Promise<PluggySyncSummaryDto> => {
  const response = await httpClient.post<PluggySyncSummaryDto>(
    API_ENDPOINTS.PLUGGY.SYNC,
    {},
    {
      headers: {
        [API_HEADERS.PLUGGY_ACCESS_TOKEN]: pluggyAccessToken,
      },
    }
  );

  return response.data;
};
