import { httpClient } from '@/shared/api/httpClient';
import { API_ENDPOINTS, API_HEADERS } from '@/shared/api/apiEndpoints';
import type { PluggyItemDto } from '../types/connections.types';
export interface PluggySyncSummaryDto {
  totalItemsSynced: number;
  totalAccountsSynced: number;
  totalCheckingTransactionsIngested: number;
  totalCardTransactionsIngested: number;
  syncedAtUtc: string;
}

export const getPluggyItemsApi = async (
  pluggyAccessToken: string,
  signal?: AbortSignal
): Promise<PluggyItemDto[]> => {
  const response = await httpClient.get<PluggyItemDto[]>(
    API_ENDPOINTS.PLUGGY.ITEMS,
    {
      signal,
      headers: {
        [API_HEADERS.PLUGGY_ACCESS_TOKEN]: pluggyAccessToken.trim(),
      },
    }
  );

  return response.data;
};

export const syncPluggyAccountsApi = async (
  pluggyAccessToken: string,
  signal?: AbortSignal
): Promise<PluggySyncSummaryDto> => {
  const response = await httpClient.post<PluggySyncSummaryDto>(
    API_ENDPOINTS.PLUGGY.SYNC,
    {},
    {
      signal,
      headers: {
        [API_HEADERS.PLUGGY_ACCESS_TOKEN]: pluggyAccessToken.trim(),
      },
    }
  );

  return response.data;
};
