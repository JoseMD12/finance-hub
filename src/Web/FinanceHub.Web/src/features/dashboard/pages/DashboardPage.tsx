import React, { useState } from 'react';
import { AlertTriangle, RotateCcw } from 'lucide-react';
import { Card } from '@/shared/components/Card/Card';
import { PageContainer } from '@/shared/components/PageContainer/PageContainer';
import { getPresetDateRange } from '@/shared/utils/datePresets';
import { useDashboardQuery } from '../hooks/useDashboardQuery';
import { DashboardFilterBar } from '../components/DashboardFilterBar';
import { DashboardSummaryCards } from '../components/DashboardSummaryCards';
import { InstitutionBalanceList } from '../components/InstitutionBalanceList';
import { CategoryRankedBars } from '../components/CategoryRankedBars';
import { MonthlyCashFlowChart } from '../components/MonthlyCashFlowChart';
import { RecentTransactionsCard } from '../components/RecentTransactionsCard';
import { DashboardSkeleton } from '../components/DashboardSkeleton';
import type { DashboardFilterParams } from '../types/dashboard.types';

function buildInitialFilters(): DashboardFilterParams {
  const range = getPresetDateRange('current-month');
  return {
    datePreset: 'current-month',
    startDate: range.startDate,
    endDate: range.endDate,
  };
}

/**
 * Página do painel: apenas composição e estado de filtro. Toda busca de dados vive no hook, e
 * todo desenho vive nos componentes de apresentação (Regra 5).
 */
export const DashboardPage: React.FC = () => {
  const [filters, setFilters] = useState<DashboardFilterParams>(buildInitialFilters);

  const { data: dashboard, isLoading, error, refetch } = useDashboardQuery(filters);

  const handleFilterChange = React.useCallback((next: Partial<DashboardFilterParams>) => {
    setFilters((previous) => ({ ...previous, ...next }));
  }, []);

  const handleResetFilters = React.useCallback(() => {
    setFilters(buildInitialFilters());
  }, []);

  const handleRetry = React.useCallback(() => {
    void refetch();
  }, [refetch]);

  // Skeleton apenas na primeira carga sem cache. Ao trocar de período, `keepPreviousData`
  // mantém os dados anteriores montados e a tela não pisca (Regra 25).
  const isInitialLoading = isLoading && !dashboard;

  return (
    <PageContainer
      title="Visão Geral e Saldos Consolidados"
      description="Monitoramento unificado de patrimônio via Open Finance e ingestão de extratos"
    >
      {isInitialLoading ? (
        <DashboardSkeleton />
      ) : (
        <div className="flex flex-col gap-6">
          <DashboardFilterBar
            filters={filters}
            onFilterChange={handleFilterChange}
            onResetFilters={handleResetFilters}
          />

          {error && !dashboard ? (
            <Card
              className="flex flex-col items-center gap-3 p-8 text-center border-status-danger/30 bg-status-danger-bg"
              hoverable={false}
            >
              <AlertTriangle className="w-6 h-6 text-status-danger" aria-hidden="true" />
              <p className="text-xs font-semibold text-status-danger">
                {error.problemDetails.detail ?? 'Não foi possível carregar as informações do painel no momento.'}
              </p>
              <button
                type="button"
                onClick={handleRetry}
                className="inline-flex items-center gap-1.5 px-4 py-2 text-xs font-semibold text-white bg-brand rounded-xl hover:bg-brand-dark transition-colors duration-200 cursor-pointer"
              >
                <RotateCcw className="w-3.5 h-3.5" aria-hidden="true" />
                Tentar novamente
              </button>
            </Card>
          ) : (
            <>
              <DashboardSummaryCards summary={dashboard?.summary} />

              <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                <InstitutionBalanceList balances={dashboard?.institutionBalances ?? []} />
                <CategoryRankedBars categories={dashboard?.categoryExpenses ?? []} />
              </div>

              <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                <MonthlyCashFlowChart points={dashboard?.monthlyCashFlow ?? []} />
                <RecentTransactionsCard transactions={dashboard?.recentTransactions ?? []} />
              </div>
            </>
          )}
        </div>
      )}
    </PageContainer>
  );
};

export default DashboardPage;
