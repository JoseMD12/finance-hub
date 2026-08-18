import { httpClient } from '@/shared/api/httpClient';
import { API_ENDPOINTS, API_HEADERS } from '@/shared/api/apiEndpoints';
import type { PluggySyncSummaryDto } from '../types/connections.types';

/**
 * Triggers synchronization of all Open Finance bank accounts and transactions via Meu.Pluggy.
 * Sends the session token securely in the X-Pluggy-Access-Token header.
 */
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
