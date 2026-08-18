import React from 'react';
import { Card } from '@/shared/components/Card/Card';
import { CheckCircle2, Landmark, CreditCard, ArrowLeftRight } from 'lucide-react';
import type { PluggySyncSummaryDto } from '../types/connections.types';

interface SyncSummaryBannerProps {
  summary: PluggySyncSummaryDto;
}

export const SyncSummaryBanner: React.FC<SyncSummaryBannerProps> = ({ summary }) => {
  const totalTransactions = summary.totalCheckingTransactionsIngested + summary.totalCardTransactionsIngested;

  return (
    <Card className="border-status-success/30 bg-status-success-bg/40">
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <div className="w-9 h-9 rounded-xl bg-status-success text-white flex items-center justify-center font-bold text-sm shadow-sm flex-shrink-0">
            <CheckCircle2 className="w-5 h-5" />
          </div>
          <div>
            <h3 className="text-xs font-bold text-slate-800">
              Sincronização Realizada com Sucesso
            </h3>
            <p className="text-[11px] text-slate-500 mt-0.5">
              Os dados bancários foram integrados e normalizados pelo TransactionAggregator.
            </p>
          </div>
        </div>

        <div className="grid grid-cols-3 gap-3 md:gap-6">
          <div className="flex items-center gap-2">
            <Landmark className="w-4 h-4 text-secondary flex-shrink-0" />
            <div className="flex flex-col">
              <span className="text-[10px] text-slate-400 font-medium">Bancos</span>
              <span className="text-xs font-bold text-slate-800">{summary.totalItemsSynced}</span>
            </div>
          </div>

          <div className="flex items-center gap-2">
            <CreditCard className="w-4 h-4 text-secondary flex-shrink-0" />
            <div className="flex flex-col">
              <span className="text-[10px] text-slate-400 font-medium">Contas</span>
              <span className="text-xs font-bold text-slate-800">{summary.totalAccountsSynced}</span>
            </div>
          </div>

          <div className="flex items-center gap-2">
            <ArrowLeftRight className="w-4 h-4 text-brand flex-shrink-0" />
            <div className="flex flex-col">
              <span className="text-[10px] text-slate-400 font-medium">Transações</span>
              <span className="text-xs font-bold text-slate-800">{totalTransactions}</span>
            </div>
          </div>
        </div>
      </div>
    </Card>
  );
};
