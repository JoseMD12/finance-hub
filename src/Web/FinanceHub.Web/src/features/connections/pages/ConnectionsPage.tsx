import React, { useEffect, useRef } from 'react';
import { motion, useReducedMotion } from 'framer-motion';
import { usePluggyToken } from '../hooks/usePluggyToken';
import { useSyncPluggyMutation } from '../hooks/useSyncPluggyMutation';
import { useConnectedInstitutionsQuery } from '../hooks/useConnectedInstitutionsQuery';
import { useDashboardQuery } from '@/features/dashboard/hooks/useDashboardQuery';
import { PluggySyncPanel } from '../components/PluggySyncPanel';
import { SyncSummaryBanner } from '../components/SyncSummaryBanner';
import { ConnectionCard } from '../components/ConnectionCard';
import { SavedInstitutionCard } from '../components/SavedInstitutionCard';
import { EmptyConnectionsState } from '../components/EmptyConnectionsState';
import { FileImporterCard } from '../components/FileImporterCard';
import { Skeleton } from '@/shared/components/Skeleton/Skeleton';
import { PageContainer } from '@/shared/components/PageContainer/PageContainer';

export const ConnectionsPage: React.FC = () => {
  const prefersReduced = useReducedMotion();
  const { token, hasToken, lastSync, saveToken, saveLastSync, clearToken } = usePluggyToken();
  const { data: items, isLoading: isLoadingItems } = useConnectedInstitutionsQuery(token);
  const { data: dashboard, isLoading: isLoadingDashboard } = useDashboardQuery();
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
  const hasPluggyItems = connectedItems.length > 0;

  // Agrupa contas salvas no banco por instituição
  const savedBalances = (dashboard?.accountBalances as any[]) ?? [];
  const groupedSavedInstitutions = React.useMemo(() => {
    const map = new Map<string, { totalBalance: number; totalCredit: number; accountsCount: number }>();

    for (const acc of savedBalances) {
      const instName = acc?.institutionName || acc?.institutionId || 'Outros';
      const balance = typeof acc?.balanceBrl === 'number' ? acc.balanceBrl : (typeof acc?.amount === 'number' ? acc.amount : 0);
      const existing = map.get(instName) || { totalBalance: 0, totalCredit: 0, accountsCount: 0 };
      if (balance >= 0) {
        existing.totalBalance += balance;
      } else {
        existing.totalCredit += Math.abs(balance);
      }
      existing.accountsCount += 1;
      map.set(instName, existing);
    }

    return Array.from(map.entries()).map(([name, data]) => ({
      name,
      totalBalance: data.totalBalance,
      totalCredit: data.totalCredit,
      accountsCount: data.accountsCount,
    }));
  }, [savedBalances]);

  const hasSavedInstitutions = groupedSavedInstitutions.length > 0;
  const hasAnyInstitutions = hasPluggyItems || hasSavedInstitutions;

  const renderInstitutionsContent = () => {
    if (isLoadingItems || (isLoadingDashboard && !hasSavedInstitutions)) {
      return (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <Skeleton className="h-32 rounded-2xl" />
          <Skeleton className="h-32 rounded-2xl" />
          <Skeleton className="h-32 rounded-2xl" />
        </div>
      );
    }

    if (hasPluggyItems) {
      return (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          {connectedItems.map((item, index) => (
            <motion.div
              key={item.id}
              initial={prefersReduced ? undefined : { opacity: 0, y: 12 }}
              animate={prefersReduced ? undefined : { opacity: 1, y: 0 }}
              transition={prefersReduced ? undefined : { delay: index * 0.08, duration: 0.3 }}
            >
              <ConnectionCard item={item} />
            </motion.div>
          ))}
        </div>
      );
    }

    if (hasSavedInstitutions) {
      return (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          {groupedSavedInstitutions.map((inst, index) => (
            <motion.div
              key={inst.name}
              initial={prefersReduced ? undefined : { opacity: 0, y: 12 }}
              animate={prefersReduced ? undefined : { opacity: 1, y: 0 }}
              transition={prefersReduced ? undefined : { delay: index * 0.08, duration: 0.3 }}
            >
              <SavedInstitutionCard
                institutionName={inst.name}
                totalBalance={inst.totalBalance}
                totalCredit={inst.totalCredit}
              />
            </motion.div>
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
        isConnected={hasToken}
        isSyncing={isSyncing}
        lastSync={lastSync}
        onSync={handleSync}
        onSaveToken={saveToken}
        onClearToken={clearToken}
      />

      <section className="flex flex-col gap-3">
        <div className="flex items-center justify-between">
          <h2 className="text-xs font-bold text-slate-500 uppercase tracking-wider">
            Instituições Conectadas {hasAnyInstitutions ? `(${hasPluggyItems ? connectedItems.length : groupedSavedInstitutions.length})` : ''}
          </h2>
        </div>

        {renderInstitutionsContent()}
      </section>

      <FileImporterCard />
    </PageContainer>
  );
};

export default ConnectionsPage;
