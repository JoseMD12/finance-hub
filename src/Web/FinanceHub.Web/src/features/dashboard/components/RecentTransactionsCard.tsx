import React from 'react';
import { Link } from 'react-router-dom';
import { ArrowUpRight } from 'lucide-react';
import { Card } from '@/shared/components/Card/Card';
import { formatCurrencyBRL, formatDateBR, formatPaymentMethod } from '@/shared/utils/formatters';
import { getInstitutionInfo } from '@/shared/constants/institutions';
import { cn } from '@/shared/utils/cn';
import type { TransactionDto } from '@/features/transactions/types/transactions.types';

export interface RecentTransactionsCardProps {
  transactions: readonly TransactionDto[];
}

const RecentTransactionsCardComponent: React.FC<RecentTransactionsCardProps> = ({ transactions }) => (
  <Card className="flex flex-col" hoverable={false}>
    <div className="flex items-center justify-between gap-3 mb-4">
      <h2 className="text-base font-bold text-secondary">Lançamentos Recentes</h2>
      <Link
        to="/transacoes"
        className="text-xs font-semibold text-brand hover:underline inline-flex items-center gap-1 transition-colors duration-200"
      >
        Ver extrato
        <ArrowUpRight className="w-3.5 h-3.5" aria-hidden="true" />
      </Link>
    </div>

    {transactions.length === 0 ? (
      <div className="p-6 text-center text-xs text-slate-400 border border-dashed border-border-subtle rounded-xl">
        Nenhum lançamento no período selecionado.
      </div>
    ) : (
      <ul className="flex flex-col divide-y divide-slate-100/80">
        {transactions.map((transaction) => {
          const isCredit = transaction.type === 'Credit';
          const institution = getInstitutionInfo(transaction.institutionId);

          return (
            <li key={transaction.id} className="flex items-center justify-between gap-3 py-2.5">
              <div className="min-w-0">
                <span className="text-xs font-semibold text-slate-800 block truncate">
                  {transaction.merchantName || transaction.description}
                </span>
                <span className="text-[11px] text-slate-400 font-medium">
                  {formatDateBR(transaction.transactionDateUtc)} · {institution.name} ·{' '}
                  {formatPaymentMethod(transaction.channel)}
                </span>
              </div>

              <span
                className={cn(
                  'text-xs font-bold tabular-nums shrink-0',
                  isCredit ? 'text-status-success' : 'text-slate-700',
                )}
              >
                {isCredit ? '+' : '-'} {formatCurrencyBRL(transaction.amount)}
              </span>
            </li>
          );
        })}
      </ul>
    )}
  </Card>
);

export const RecentTransactionsCard = React.memo(RecentTransactionsCardComponent);
