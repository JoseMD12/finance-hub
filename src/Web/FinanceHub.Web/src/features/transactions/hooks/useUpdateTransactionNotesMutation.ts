import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { updateTransactionNotesApi } from '../api/transactionsApi';
import { transactionsKeys } from '../api/transactionsKeys';
import { showApiError } from '@/shared/utils/apiError';

export const useUpdateTransactionNotesMutation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: { transactionId: string; notes: string | null }) =>
      updateTransactionNotesApi(payload),
    onSuccess: (_, variables) => {
      toast.success(variables.notes ? 'Nota salva com sucesso' : 'Nota removida');
      queryClient.invalidateQueries({ queryKey: transactionsKeys.all });
    },
    onError: (error) => {
      showApiError(error, 'Falha ao salvar nota da transação');
    },
  });
};
