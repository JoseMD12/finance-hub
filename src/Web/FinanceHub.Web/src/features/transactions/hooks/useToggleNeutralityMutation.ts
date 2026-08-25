import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { transactionsKeys } from '../api/transactionsKeys';
import { toggleTransactionNeutralityApi } from '../api/transactionsApi';
import { showApiError } from '@/shared/utils/apiError';
import type { ToggleNeutralityPayload } from '../types/transactions.types';
import type { ApiError } from '@/shared/types/api.types';

export function useToggleNeutralityMutation() {
  const queryClient = useQueryClient();

  return useMutation<void, ApiError, ToggleNeutralityPayload>({
    mutationFn: (payload) => toggleTransactionNeutralityApi(payload),
    onSuccess: (_, variables) => {
      toast.success(
        variables.isIgnoredInTotals
          ? 'Lançamento marcado como neutro (ignorado nos totais).'
          : 'Lançamento reativado no fluxo operacional com sucesso.'
      );
      queryClient.invalidateQueries({ queryKey: transactionsKeys.all });
    },
    onError: (error) => {
      showApiError(error);
    },
  });
}
