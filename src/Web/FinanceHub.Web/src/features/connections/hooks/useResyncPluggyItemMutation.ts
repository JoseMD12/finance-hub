import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { connectionKeys } from '../api/connectionKeys';
import { resyncPluggyItemApi } from '../api/connectionsApi';
import { showApiError } from '@/shared/utils/apiError';
import type { PluggySyncSummaryDto } from '../types/connections.types';
import type { ApiError } from '@/shared/types/api.types';

interface ResyncPluggyItemPayload {
  itemId: string;
  token: string;
}

export function useResyncPluggyItemMutation() {
  const queryClient = useQueryClient();

  return useMutation<PluggySyncSummaryDto, ApiError, ResyncPluggyItemPayload>({
    mutationFn: ({ itemId, token }) => resyncPluggyItemApi(itemId, token),
    onSuccess: () => {
      toast.success('Ressincronização da instituição iniciada.');
      queryClient.invalidateQueries({ queryKey: connectionKeys.all });
    },
    onError: (error) => {
      showApiError(error, 'Não foi possível ressincronizar esta instituição.');
    },
  });
}
