import type { DashboardFilterParams } from '../types/dashboard.types';

/**
 * Query Key Factory do Dashboard (Regra 2 de TanStack Query).
 * A chave inclui os filtros para que cada período tenha sua própria entrada de cache.
 */
export const dashboardKeys = {
  all: ['dashboard'] as const,
  summaries: () => [...dashboardKeys.all, 'summary'] as const,
  summary: (filters: DashboardFilterParams) => [...dashboardKeys.summaries(), filters] as const,
};
