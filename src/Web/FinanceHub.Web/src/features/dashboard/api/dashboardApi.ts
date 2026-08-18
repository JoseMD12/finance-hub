import { httpClient } from '@/shared/api/httpClient';
import { API_ENDPOINTS } from '@/shared/api/apiEndpoints';
import type { DashboardSummaryDto } from '../types/dashboard.types';

export const getDashboardSummaryApi = async (signal?: AbortSignal): Promise<DashboardSummaryDto> => {
  const response = await httpClient.get<DashboardSummaryDto>(API_ENDPOINTS.DASHBOARD.SUMMARY, { signal });
  return response.data;
};
