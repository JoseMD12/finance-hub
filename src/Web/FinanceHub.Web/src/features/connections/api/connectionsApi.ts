import { httpClient } from '@/shared/api/httpClient';
import { API_ENDPOINTS, API_HEADERS } from '@/shared/api/apiEndpoints';
import type {
  PluggyItemDto,
  SyncJobAcceptedDto,
  SyncJobStatusDto,
} from '../types/connections.types';

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
): Promise<SyncJobAcceptedDto> => {
  const response = await httpClient.post<SyncJobAcceptedDto>(
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

export const getSyncJobStatusApi = async (
  jobId: string,
  signal?: AbortSignal
): Promise<SyncJobStatusDto> => {
  const response = await httpClient.get<SyncJobStatusDto>(
    API_ENDPOINTS.PLUGGY.SYNC_JOB_STATUS(jobId),
    {
      signal,
    }
  );

  return response.data;
};
