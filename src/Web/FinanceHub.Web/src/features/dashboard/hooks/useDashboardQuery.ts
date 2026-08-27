import { useQuery, keepPreviousData } from '@tanstack/react-query';
import { dashboardKeys } from '../api/dashboardKeys';
import { getDashboardSummaryApi } from '../api/dashboardApi';
import type { DashboardFilterParams, DashboardSummaryDto } from '../types/dashboard.types';
import type { ApiError } from '@/shared/types/api.types';

/** Frescor de saldos consolidados, conforme a tabela de caching da Regra 3. */
const STALE_TIME_MS = 1000 * 60;
const GC_TIME_MS = 1000 * 60 * 10;

export function useDashboardQuery(filters: DashboardFilterParams = {}) {
  return useQuery<DashboardSummaryDto, ApiError>({
    queryKey: dashboardKeys.summary(filters),
    queryFn: ({ signal }) => getDashboardSummaryApi(filters, signal),
    staleTime: STALE_TIME_MS,
    gcTime: GC_TIME_MS,
    // Mantém os dados anteriores montados ao trocar de período, evitando que o layout
    // reflua e que as animações de entrada disparem de novo a cada filtro (Regra 25).
    placeholderData: keepPreviousData,
  });
}
