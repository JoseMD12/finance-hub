import React, { useEffect, useRef } from 'react';
import { usePluggyToken } from '../hooks/usePluggyToken';
import { useSyncPluggyMutation } from '../hooks/useSyncPluggyMutation';
import { useConnectedInstitutionsQuery } from '../hooks/useConnectedInstitutionsQuery';
import { PluggySyncPanel } from '../components/PluggySyncPanel';
import { SyncSummaryBanner } from '../components/SyncSummaryBanner';
import { ConnectionCard } from '../components/ConnectionCard';
import { EmptyConnectionsState } from '../components/EmptyConnectionsState';
import { FileImporterCard } from '../components/FileImporterCard';
import { Skeleton } from '@/shared/components/Skeleton/Skeleton';
import { PageContainer } from '@/shared/components/PageContainer/PageContainer';

export const ConnectionsPage: React.FC = () => {
  const { token, hasToken, lastSync, saveToken, saveLastSync, clearToken } = usePluggyToken();
  const { data: items, isLoading: isLoadingItems } = useConnectedInstitutionsQuery(token);
  const autoSyncTokenRef = useRef<string | null>(null);

  const { mutate: syncAccounts, isPending: isSyncing } = useSyncPluggyMutation({
    onSyncSuccess: (summary) => {
      saveLastSync(summary);
    },
  });

  const handleSync = (targetToken: string) => {
    saveToken(targetToken);
    syncAccounts(targetToken);
  };

  useEffect(() => {
    if (!token) {
      autoSyncTokenRef.current = null;
      return;
    }

    const hasConnectedItems = Boolean(items?.length);
    const alreadyAutoSynced = autoSyncTokenRef.current === token;

    if (!hasConnectedItems || lastSync || isSyncing || alreadyAutoSynced) {
      return;
    }

    autoSyncTokenRef.current = token;
    syncAccounts(token);
  }, [isSyncing, items, lastSync, syncAccounts, token]);

  const connectedItems = items ?? [];
  const hasInstitutions = connectedItems.length > 0;

  const renderInstitutionsContent = () => {
    if (isLoadingItems) {
      return (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <Skeleton className="h-32 rounded-2xl" />
          <Skeleton className="h-32 rounded-2xl" />
          <Skeleton className="h-32 rounded-2xl" />
        </div>
      );
    }

    if (hasInstitutions) {
      return (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          {connectedItems.map((item) => (
            <ConnectionCard
              key={item.id}
              item={item}
            />
          ))}
        </div>
      );
    }

    return <EmptyConnectionsState hasToken={hasToken} />;
  };

  return (
    <PageContainer
      title="Conexões"
      description="Instituições bancárias e extratos conectados"
    >
      {lastSync && <SyncSummaryBanner summary={lastSync} />}

      <PluggySyncPanel
        token={token}
        isConnected={hasInstitutions || Boolean(lastSync)}
        isSyncing={isSyncing}
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

        {renderInstitutionsContent()}
      </section>

      <FileImporterCard />
    </PageContainer>
  );
};

export default ConnectionsPage;
