import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { transactionsKeys } from '../api/transactionsKeys';
import { categorizeTransactionApi } from '../api/transactionsApi';
import { showApiError } from '@/shared/utils/apiError';
import type { CategorizeTransactionPayload } from '../types/transactions.types';
import type { ApiError } from '@/shared/types/api.types';

export function useCategorizeTransactionMutation() {
  const queryClient = useQueryClient();

  return useMutation<void, ApiError, CategorizeTransactionPayload>({
    mutationFn: (payload) => categorizeTransactionApi(payload),
    onSuccess: () => {
      toast.success('Categoria atualizada com sucesso!');
      queryClient.invalidateQueries({ queryKey: transactionsKeys.all });
    },
    onError: (error) => {
      showApiError(error);
    },
  });
}
