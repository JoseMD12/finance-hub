import { httpClient } from '@/shared/api/httpClient';
import { API_ENDPOINTS } from '@/shared/api/apiEndpoints';
import type { DashboardFilterParams, DashboardSummaryDto } from '../types/dashboard.types';

export const getDashboardSummaryApi = async (
  filters: DashboardFilterParams = {},
  signal?: AbortSignal,
): Promise<DashboardSummaryDto> => {
  const response = await httpClient.get<DashboardSummaryDto>(API_ENDPOINTS.DASHBOARD.SUMMARY, {
    signal,
    params: {
      startDate: filters.startDate,
      endDate: filters.endDate,
      institutionId: filters.institutionId || undefined,
      includeIgnoredInTotals: filters.includeIgnoredInTotals || undefined,
    },
  });

  return response.data;
};
