import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { connectionKeys } from '../api/connectionKeys';
import { syncPluggyAccountsApi, getSyncJobStatusApi } from '../api/connectionsApi';
import { showApiError } from '@/shared/utils/apiError';
import type { PluggySyncSummaryDto } from '../types/connections.types';
import type { ApiError } from '@/shared/types/api.types';

interface UseSyncPluggyMutationOptions {
  onSyncSuccess?: (summary: PluggySyncSummaryDto) => void;
}

const MAX_POLL_ATTEMPTS = 30;
const POLL_INTERVAL_MS = 1000;

export function useSyncPluggyMutation(options?: UseSyncPluggyMutationOptions) {
  const queryClient = useQueryClient();

  return useMutation<PluggySyncSummaryDto, ApiError, string>({
    mutationFn: async (token: string) => {
      const accepted = await syncPluggyAccountsApi(token);
      toast.info('Sincronização iniciada em segundo plano...');

      for (let attempt = 0; attempt < MAX_POLL_ATTEMPTS; attempt++) {
        await new Promise((resolve) => setTimeout(resolve, POLL_INTERVAL_MS));

        const job = await getSyncJobStatusApi(accepted.jobId);
        if (job.status === 'Completed' && job.result) {
          return job.result;
        }

        if (job.status === 'Failed') {
          throw new Error(job.errorMessage || 'Falha ao processar sincronização em segundo plano.');
        }
      }

      throw new Error('A sincronização excedeu o tempo limite de resposta.');
    },
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
