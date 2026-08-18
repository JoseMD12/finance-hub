import { useQuery } from '@tanstack/react-query';
import { dashboardKeys } from '../api/dashboardKeys';
import { getDashboardSummaryApi } from '../api/dashboardApi';
import type { DashboardSummaryDto } from '../types/dashboard.types';
import type { ApiError } from '@/shared/types/api.types';

export function useDashboardQuery() {
  return useQuery<DashboardSummaryDto, ApiError>({
    queryKey: dashboardKeys.summary(),
    queryFn: ({ signal }) => getDashboardSummaryApi(signal),
    staleTime: 1000 * 60, // 1 minuto
  });
}
