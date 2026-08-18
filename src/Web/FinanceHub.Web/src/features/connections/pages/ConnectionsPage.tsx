import React from 'react';
import { usePluggyToken } from '../hooks/usePluggyToken';
import { useSyncPluggyMutation } from '../hooks/useSyncPluggyMutation';
import { useConnectedAccountsQuery } from '../hooks/useConnectedAccountsQuery';
import { PluggySyncPanel } from '../components/PluggySyncPanel';
import { SyncSummaryBanner } from '../components/SyncSummaryBanner';
import { ConnectionCard } from '../components/ConnectionCard';
import { EmptyConnectionsState } from '../components/EmptyConnectionsState';
import { FileImporterCard } from '../components/FileImporterCard';
import { Skeleton } from '@/shared/components/Skeleton/Skeleton';

export const ConnectionsPage: React.FC = () => {
  const { token, hasToken, lastSync, saveToken, saveLastSync, clearToken } = usePluggyToken();
  const { data: accounts, isLoading: isLoadingAccounts } = useConnectedAccountsQuery();

  const syncMutation = useSyncPluggyMutation({
    onSyncSuccess: (summary) => {
      saveLastSync(summary);
    },
  });

  const handleSync = (targetToken: string) => {
    saveToken(targetToken);
    syncMutation.mutate(targetToken);
  };

  const connectedAccounts = accounts ?? [];
  const hasAccounts = connectedAccounts.length > 0;

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-1">
        <h1 className="text-xl font-extrabold text-secondary">
          Conexões
        </h1>
        <p className="text-xs text-slate-500 font-medium">
          Gerenciamento de integrações Open Finance via Meu.Pluggy e ingestão de extratos
        </p>
      </div>

      <PluggySyncPanel
        token={token}
        isSyncing={syncMutation.isPending}
        lastSync={lastSync}
        onSync={handleSync}
        onSaveToken={saveToken}
        onClearToken={clearToken}
      />

      {lastSync && <SyncSummaryBanner summary={lastSync} />}

      <section className="flex flex-col gap-3">
        <div className="flex items-center justify-between">
          <h2 className="text-xs font-bold text-slate-500 uppercase tracking-wider">
            Instituições Conectadas {hasAccounts ? `(${connectedAccounts.length})` : ''}
          </h2>
        </div>

        {isLoadingAccounts ? (
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <Skeleton className="h-32 rounded-2xl" />
            <Skeleton className="h-32 rounded-2xl" />
            <Skeleton className="h-32 rounded-2xl" />
          </div>
        ) : hasAccounts ? (
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            {connectedAccounts.map((account) => (
              <ConnectionCard key={`${account.institutionName}-${account.accountNumber}`} account={account} />
            ))}
          </div>
        ) : (
          <EmptyConnectionsState hasToken={hasToken} />
        )}
      </section>

      <FileImporterCard />
    </div>
  );
};

export default ConnectionsPage;
