import React from 'react';
import { Card } from '@/shared/components/Card/Card';
import { formatCurrencyBRL } from '@/shared/utils/formatters';
import { ArrowUpRight, ArrowDownRight, Wallet } from 'lucide-react';
import type { TransactionSummaryDto } from '../types/transactions.types';

export interface TransactionsSummaryCardsProps {
  summary?: TransactionSummaryDto;
  isLoading?: boolean;
}

export const TransactionsSummaryCards: React.FC<TransactionsSummaryCardsProps> = ({
  summary,
  isLoading,
}) => {
  if (isLoading) {
    return (
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4 animate-pulse">
        {[1, 2, 3].map((i) => (
          <div key={i} className="h-24 rounded-2xl bg-slate-200" />
        ))}
      </div>
    );
  }

  const income = summary?.totalIncome ?? 0;
  const expense = summary?.totalExpense ?? 0;
  const net = summary?.netBalance ?? 0;

  return (
    <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
      {/* Entradas */}
      <Card className="p-4 flex items-center justify-between bg-surface-card">
        <div className="flex flex-col gap-1">
          <span className="text-xs font-semibold text-slate-500">Total de Entradas</span>
          <span className="text-lg font-black text-status-success">
            + {formatCurrencyBRL(income)}
          </span>
        </div>
        <div className="w-10 h-10 rounded-xl bg-status-success-bg flex items-center justify-center text-status-success">
          <ArrowUpRight className="w-5 h-5" aria-hidden="true" />
        </div>
      </Card>

      {/* Saídas */}
      <Card className="p-4 flex items-center justify-between bg-surface-card">
        <div className="flex flex-col gap-1">
          <span className="text-xs font-semibold text-slate-500">Total de Saídas</span>
          <span className="text-lg font-black text-status-danger">
            - {formatCurrencyBRL(expense)}
          </span>
        </div>
        <div className="w-10 h-10 rounded-xl bg-status-danger-bg flex items-center justify-center text-status-danger">
          <ArrowDownRight className="w-5 h-5" aria-hidden="true" />
        </div>
      </Card>

      {/* Saldo Líquido do Período */}
      <Card className="p-4 flex items-center justify-between bg-surface-card">
        <div className="flex flex-col gap-1">
          <span className="text-xs font-semibold text-slate-500">Saldo Líquido</span>
          <span
            className={`text-lg font-black ${
              net >= 0 ? 'text-brand-dark' : 'text-status-danger'
            }`}
          >
            {net >= 0 ? '+ ' : '- '}
            {formatCurrencyBRL(Math.abs(net))}
          </span>
        </div>
        <div className="w-10 h-10 rounded-xl bg-brand-light flex items-center justify-center text-brand">
          <Wallet className="w-5 h-5" aria-hidden="true" />
        </div>
      </Card>
    </div>
  );
};
