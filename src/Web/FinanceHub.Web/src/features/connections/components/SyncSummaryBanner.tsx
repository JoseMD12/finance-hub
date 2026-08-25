import { Card } from '@/shared/components/Card/Card';
import { IconCircle } from '@/shared/components/IconCircle/IconCircle';
import { ArrowLeftRight, CheckCircle2, Clock, CreditCard, Landmark } from 'lucide-react';
import React from 'react';
import type { PluggySyncSummaryDto } from '../types/connections.types';

interface SyncSummaryBannerProps {
  summary: PluggySyncSummaryDto;
}

export const SyncSummaryBanner: React.FC<SyncSummaryBannerProps> = ({ summary }) => {
  const totalTransactions = summary.totalCheckingTransactionsIngested + summary.totalCardTransactionsIngested;

  return (
    <Card className="border-status-success/30 bg-status-success-bg/40 py-3.5 px-4 md:px-5 flex flex-col gap-2.5">
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

      <div className="flex h-full items-center gap-1.5 pt-2 border-t border-emerald-500/15 text-[11px] text-slate-500">
        <Clock className="w-3.5 h-3.5 text-slate-400 shrink-0 self-center" aria-hidden="true" />
        <span className="inline-flex items-center self-center leading-none">
          Compras no cartão podem demorar até 48h para entrar no seu histórico.
        </span>
      </div>
    </Card>
  );
};
