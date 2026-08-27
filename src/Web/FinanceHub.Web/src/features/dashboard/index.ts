/**
 * Fronteira pública da feature Dashboard (Regra 2.1).
 *
 * Outras features consomem apenas o que está aqui. Importar subpastas internas
 * (`@/features/dashboard/hooks/...`) é proibido pela Regra 2.2.
 */
export { default as DashboardPage } from './pages/DashboardPage';
export { useDashboardQuery } from './hooks/useDashboardQuery';
export type {
  DashboardSummaryDto,
  DashboardFilterParams,
  InstitutionBalanceDto,
  CategoryExpenseDto,
  MonthlyCashFlowPointDto,
} from './types/dashboard.types';
