import React from 'react';
import { usePluggyToken } from '../hooks/usePluggyToken';
import { useSyncPluggyMutation } from '../hooks/useSyncPluggyMutation';
import { useConnectedInstitutionsQuery } from '../hooks/useConnectedInstitutionsQuery';
import { PluggySyncPanel } from '../components/PluggySyncPanel';
import { SyncSummaryBanner } from '../components/SyncSummaryBanner';
import { ConnectionCard } from '../components/ConnectionCard';
import { EmptyConnectionsState } from '../components/EmptyConnectionsState';
import { FileImporterCard } from '../components/FileImporterCard';
import { Skeleton } from '@/shared/components/Skeleton/Skeleton';

export const ConnectionsPage: React.FC = () => {
  const { token, hasToken, lastSync, saveToken, saveLastSync, clearToken } = usePluggyToken();
  const { data: items, isLoading: isLoadingItems } = useConnectedInstitutionsQuery(token);

  const syncMutation = useSyncPluggyMutation({
    onSyncSuccess: (summary) => {
      saveLastSync(summary);
    },
  });

  const handleSync = (targetToken: string) => {
    saveToken(targetToken);
    syncMutation.mutate(targetToken);
  };


  const connectedItems = items ?? [];
  const hasInstitutions = connectedItems.length > 0;

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-1">
        <h1 className="text-xl font-extrabold text-secondary">
          Conexões
        </h1>
        <p className="text-xs text-slate-500 font-medium">
          Instituições bancárias e extratos conectados
        </p>
      </div>

      {lastSync && <SyncSummaryBanner summary={lastSync} />}

      <PluggySyncPanel
        token={token}
        isConnected={hasInstitutions || Boolean(lastSync)}
        isSyncing={syncMutation.isPending}
        lastSync={lastSync}
        onSync={handleSync}
        onSaveToken={saveToken}
        onClearToken={clearToken}
      />

      <section className="flex flex-col gap-3">
        <div className="flex items-center justify-between">
          <h2 className="text-xs font-bold text-slate-500 uppercase tracking-wider">
            Instituições Conectadas {hasInstitutions ? `(${connectedItems.length})` : ''}
          </h2>
        </div>

        {isLoadingItems ? (
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <Skeleton className="h-32 rounded-2xl" />
            <Skeleton className="h-32 rounded-2xl" />
            <Skeleton className="h-32 rounded-2xl" />
          </div>
        ) : hasInstitutions ? (
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            {connectedItems.map((item) => (
              <ConnectionCard
                key={item.id}
                item={item}
              />
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
