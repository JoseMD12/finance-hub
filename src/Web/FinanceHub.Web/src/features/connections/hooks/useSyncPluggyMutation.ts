import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { connectionKeys } from '../api/connectionKeys';
import { syncPluggyAccountsApi } from '../api/connectionsApi';
import { showApiError } from '@/shared/utils/apiError';
import type { PluggySyncSummaryDto } from '../types/connections.types';
import type { ApiError } from '@/shared/types/api.types';

interface UseSyncPluggyMutationOptions {
  onSyncSuccess?: (summary: PluggySyncSummaryDto) => void;
}

export function useSyncPluggyMutation(options?: UseSyncPluggyMutationOptions) {
  const queryClient = useQueryClient();

  return useMutation<PluggySyncSummaryDto, ApiError, string>({
    mutationFn: (token: string) => syncPluggyAccountsApi(token),
    onSuccess: (summary) => {
      toast.success(
        `Sincronização concluída! ${summary.totalItemsSynced} instituição(ões), ${summary.totalAccountsSynced} conta(s) e ${summary.totalCheckingTransactionsIngested + summary.totalCardTransactionsIngested} transações atualizadas.`
      );
      
      options?.onSyncSuccess?.(summary);

      // Invalidação cirúrgica de caches relacionados
      queryClient.invalidateQueries({ queryKey: connectionKeys.all });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
      queryClient.invalidateQueries({ queryKey: ['transactions'] });
    },
    onError: (error) => {
      showApiError(error, 'Não foi possível sincronizar as contas do Meu.Pluggy no momento.');
    },
  });
}
