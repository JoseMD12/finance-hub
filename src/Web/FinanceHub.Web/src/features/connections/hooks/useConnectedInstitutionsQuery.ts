import { useQuery } from '@tanstack/react-query';
import { connectionKeys } from '../api/connectionKeys';
import { getPluggyItemsApi } from '../api/connectionsApi';
import { CONNECTIONS_DEFAULTS } from '../constants/connectionsConstants';
import type { PluggyItemDto } from '../types/connections.types';
import type { ApiError } from '@/shared/types/api.types';

export function useConnectedInstitutionsQuery(token: string) {
  const cleanToken = token.trim();

  return useQuery<PluggyItemDto[], ApiError>({
    queryKey: connectionKeys.items(cleanToken),
    queryFn: ({ signal }) => getPluggyItemsApi(cleanToken, signal),
    enabled: Boolean(cleanToken),
    staleTime: CONNECTIONS_DEFAULTS.STALE_TIME_MS,
  });
}
