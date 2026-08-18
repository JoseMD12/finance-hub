import { useQuery } from '@tanstack/react-query';
import { connectionKeys } from '../api/connectionKeys';
import { getDashboardSummaryApi } from '@/features/dashboard/api/dashboardApi';
import { CONNECTIONS_DEFAULTS } from '../constants/connectionsConstants';
import type { AccountBalanceDto } from '@/features/dashboard/types/dashboard.types';
import type { ApiError } from '@/shared/types/api.types';

export function useConnectedAccountsQuery() {
  return useQuery<AccountBalanceDto[], ApiError>({
    queryKey: connectionKeys.accounts(),
    queryFn: async ({ signal }) => {
      const summary = await getDashboardSummaryApi(signal);
      return summary.accountBalances ?? [];
    },
    staleTime: CONNECTIONS_DEFAULTS.STALE_TIME_MS,
  });
}
