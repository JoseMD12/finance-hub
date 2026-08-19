import React from 'react';
import { Card } from '@/shared/components/Card/Card';
import { CheckCircle2, Landmark, CreditCard, ArrowLeftRight } from 'lucide-react';
import type { PluggySyncSummaryDto } from '../types/connections.types';
import { IconCircle } from '@/shared/components/IconCircle/IconCircle';

interface SyncSummaryBannerProps {
  summary: PluggySyncSummaryDto;
}

export const SyncSummaryBanner: React.FC<SyncSummaryBannerProps> = ({ summary }) => {
  const totalTransactions = summary.totalCheckingTransactionsIngested + summary.totalCardTransactionsIngested;

  return (
    <Card className="border-status-success/30 bg-status-success-bg/40 py-3.5 px-4 md:px-5">
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-3 md:gap-4">
        <div className="flex items-center gap-3">
          <IconCircle icon={CheckCircle2} tone="success" size="md" />
          <div>
            <h3 className="text-xs font-bold text-slate-800">
              Sincronização Realizada com Sucesso
            </h3>
            <p className="text-[11px] text-slate-500">
              Suas contas e transações foram atualizadas com sucesso.
            </p>
          </div>
        </div>

        <div className="flex flex-wrap items-center gap-4 md:gap-6 pl-11 md:pl-0">
          <div className="flex items-center gap-1.5 text-xs">
            <Landmark className="w-4 h-4 text-secondary flex-shrink-0" />
            <span className="text-slate-500 font-medium">Bancos:</span>
            <strong className="font-bold text-slate-800">{summary.totalItemsSynced}</strong>
          </div>

          <div className="flex items-center gap-1.5 text-xs">
            <CreditCard className="w-4 h-4 text-secondary flex-shrink-0" />
            <span className="text-slate-500 font-medium">Contas:</span>
            <strong className="font-bold text-slate-800">{summary.totalAccountsSynced}</strong>
          </div>

          <div className="flex items-center gap-1.5 text-xs">
            <ArrowLeftRight className="w-4 h-4 text-brand flex-shrink-0" />
            <span className="text-slate-500 font-medium">Transações:</span>
            <strong className="font-bold text-slate-800">{totalTransactions}</strong>
          </div>
        </div>
      </div>
    </Card>
  );
};
