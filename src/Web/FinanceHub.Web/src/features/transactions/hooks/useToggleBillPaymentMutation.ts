import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { transactionsKeys } from '../api/transactionsKeys';
import { toggleTransactionBillPaymentApi } from '../api/transactionsApi';
import { showApiError } from '@/shared/utils/apiError';
import type { ToggleBillPaymentPayload } from '../types/transactions.types';
import type { ApiError } from '@/shared/types/api.types';

export function useToggleBillPaymentMutation() {
  const queryClient = useQueryClient();

  return useMutation<void, ApiError, ToggleBillPaymentPayload>({
    mutationFn: (payload) => toggleTransactionBillPaymentApi(payload),
    onSuccess: (_, variables) => {
      toast.success(
        variables.isBillPayment
          ? 'Transação marcada como fatura de cartão (ignorada nos totais operacionais).'
          : 'Transação removida da marcação de fatura.'
      );
      queryClient.invalidateQueries({ queryKey: transactionsKeys.all });
    },
    onError: (error) => {
      showApiError(error);
    },
  });
}
