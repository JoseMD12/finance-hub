import React from 'react';
import { Card } from '@/shared/components/Card/Card';
import { Skeleton } from '@/shared/components/Skeleton/Skeleton';
import { formatCurrencyBRL, formatDateBR, maskSensitiveAccount } from '@/shared/utils/formatters';
import { Landmark, ArrowUpRight, ArrowDownRight, Eye, SearchX } from 'lucide-react';
import { CategoryTagPopover } from './CategoryTagPopover';
import type { TransactionDto } from '../types/transactions.types';

export interface TransactionsTableProps {
  transactions: TransactionDto[];
  isLoading: boolean;
  onSelectTransaction: (transaction: TransactionDto) => void;
}

const getInstitutionBadgeStyle = (institutionId: string) => {
  const normalized = institutionId.toLowerCase();
  if (normalized.includes('itau')) {
    return 'bg-amber-50 text-amber-800 border-amber-200';
  }
  if (normalized.includes('inter')) {
    return 'bg-orange-50 text-orange-800 border-orange-200';
  }
  if (normalized.includes('mercadopago') || normalized.includes('mp')) {
    return 'bg-sky-50 text-sky-800 border-sky-200';
  }
  return 'bg-surface-ground text-slate-700 border-border-subtle';
};

export const TransactionsTable: React.FC<TransactionsTableProps> = ({
  transactions,
  isLoading,
  onSelectTransaction,
}) => {
  return (
    <Card className="p-0 overflow-hidden bg-surface-card border border-border-subtle shadow-card" hoverable={false}>
      <div className="overflow-x-auto">
        <table className="w-full text-left text-xs">
          <thead>
            <tr className="bg-secondary text-white font-semibold uppercase tracking-wider text-[11px]">
              <th className="px-6 py-4">Data</th>
              <th className="px-6 py-4">Descrição e Estabelecimento</th>
              <th className="px-6 py-4">Instituição e Conta</th>
              <th className="px-6 py-4">Categoria (Tag)</th>
              <th className="px-6 py-4">Canal</th>
              <th className="px-6 py-4 text-right">Valor</th>
              <th className="px-6 py-4 text-center">Ações</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border-subtle bg-surface-card">
            {isLoading ? (
              // Structured Skeleton Loading (5 rows)
              Array.from({ length: 5 }).map((_, idx) => (
                <tr key={`skeleton-${idx}`} className="animate-pulse">
                  <td className="px-6 py-4">
                    <Skeleton className="h-4 w-20 rounded-md" />
                  </td>
                  <td className="px-6 py-4">
                    <div className="flex flex-col gap-1.5">
                      <Skeleton className="h-4 w-40 rounded-md" />
                      <Skeleton className="h-3 w-24 rounded-md" />
                    </div>
                  </td>
                  <td className="px-6 py-4">
                    <div className="flex flex-col gap-1.5">
                      <Skeleton className="h-4 w-28 rounded-md" />
                      <Skeleton className="h-3 w-20 rounded-md" />
                    </div>
                  </td>
                  <td className="px-6 py-4">
                    <Skeleton className="h-6 w-28 rounded-full" />
                  </td>
                  <td className="px-6 py-4">
                    <Skeleton className="h-5 w-16 rounded-md" />
                  </td>
                  <td className="px-6 py-4 text-right">
                    <div className="flex justify-end">
                      <Skeleton className="h-4 w-24 rounded-md" />
                    </div>
                  </td>
                  <td className="px-6 py-4 text-center">
                    <div className="flex justify-center">
                      <Skeleton className="h-7 w-7 rounded-lg" />
                    </div>
                  </td>
                </tr>
              ))
            ) : transactions.length === 0 ? (
              // Rich Empty State
              <tr>
                <td colSpan={7} className="px-6 py-16 text-center">
                  <div className="flex flex-col items-center justify-center gap-3 max-w-sm mx-auto">
                    <div className="w-12 h-12 rounded-2xl bg-surface-ground border border-border-subtle flex items-center justify-center text-slate-400">
                      <SearchX className="w-6 h-6" aria-hidden="true" />
                    </div>
                    <div className="flex flex-col gap-1">
                      <span className="text-sm font-bold text-slate-700">
                        Nenhuma transação encontrada
                      </span>
                      <p className="text-xs text-slate-400">
                        Não encontramos lançamentos para os filtros ou busca informados. Tente ajustar os parâmetros.
                      </p>
                    </div>
                  </div>
                </td>
              </tr>
            ) : (
              transactions.map((t) => {
                const institutionStyle = getInstitutionBadgeStyle(t.institutionId);

                return (
                  <tr
                    key={t.id}
                    className="hover:bg-brand-light/20 transition-colors duration-150 group"
                  >
                    <td className="px-6 py-4 text-slate-500 font-semibold tabular-nums">
                      {formatDateBR(t.transactionDateUtc)}
                    </td>
                    <td className="px-6 py-4">
                      <div className="flex flex-col">
                        <span className="font-bold text-slate-800 group-hover:text-secondary transition-colors">
                          {t.description}
                        </span>
                        {t.merchantName && (
                          <span className="text-[11px] text-slate-400 font-medium">
                            {t.merchantName}
                          </span>
                        )}
                      </div>
                    </td>
                    <td className="px-6 py-4 text-slate-600">
                      <div className="flex flex-col gap-1">
                        <span
                          className={`inline-flex items-center gap-1.5 px-2 py-0.5 rounded-md text-[11px] font-bold border w-fit ${institutionStyle}`}
                        >
                          <Landmark className="w-3 h-3" aria-hidden="true" />
                          {t.institutionId.toUpperCase()}
                        </span>
                        <span className="text-[10px] text-slate-400 font-mono">
                          Conta {maskSensitiveAccount(t.accountNumber)}
                        </span>
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <CategoryTagPopover
                        transactionId={t.id}
                        currentCategoryId={t.categoryId}
                      />
                    </td>
                    <td className="px-6 py-4">
                      <span className="inline-flex items-center px-2 py-0.5 rounded-md bg-surface-ground border border-border-subtle text-[11px] font-mono text-slate-600">
                        {t.channel || 'Geral'}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-right font-bold text-sm">
                      <span
                        className={`tabular-nums tracking-tight font-mono inline-flex items-center gap-1 ${
                          t.type === 'Credit'
                            ? 'text-status-success'
                            : 'text-status-danger'
                        }`}
                      >
                        {t.type === 'Credit' ? (
                          <ArrowUpRight className="w-4 h-4" aria-hidden="true" />
                        ) : (
                          <ArrowDownRight className="w-4 h-4" aria-hidden="true" />
                        )}
                        {t.type === 'Credit' ? '+ ' : '- '}
                        {formatCurrencyBRL(t.amount)}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-center">
                      <button
                        type="button"
                        onClick={() => onSelectTransaction(t)}
                        aria-label={`Ver detalhes da transação ${t.description}`}
                        title="Ver Detalhes"
                        className="p-2 text-slate-400 hover:text-brand hover:bg-brand-light rounded-lg transition-all duration-200 cursor-pointer"
                      >
                        <Eye className="w-4 h-4" />
                      </button>
                    </td>
                  </tr>
                );
              })
            )}
          </tbody>
        </table>
      </div>
    </Card>
  );
};
