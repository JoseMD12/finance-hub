import React, { useState } from 'react';
import { Card } from '@/shared/components/Card/Card';
import { Skeleton } from '@/shared/components/Skeleton/Skeleton';
import { formatCurrencyBRL, formatDateBR, formatTimeBR, formatPaymentMethod, maskSensitiveAccount } from '@/shared/utils/formatters';
import { getInstitutionInfo } from '@/shared/constants/institutions';
import { cn } from '@/shared/utils/cn';
import { Landmark, ArrowUpRight, ArrowDownRight, Eye, SearchX } from 'lucide-react';
import { CategoryTagPopover } from './CategoryTagPopover';
import type { TransactionDto } from '../types/transactions.types';

export interface TransactionsTableProps {
  transactions: TransactionDto[];
  isLoading: boolean;
  onSelectTransaction: (transaction: TransactionDto) => void;
}

const BankLogoTag: React.FC<{ institutionId: string }> = ({ institutionId }) => {
  const info = getInstitutionInfo(institutionId);
  const [hasError, setHasError] = useState(false);

  return (
    <span
      className={cn(
        'inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md text-[11px] font-bold border whitespace-nowrap shrink-0 shadow-2xs',
        info.tagClass
      )}
    >
      {info.logoUrl && !hasError ? (
        <img
          src={info.logoUrl}
          alt={`Logo ${info.name}`}
          className="w-3.5 h-3.5 object-contain shrink-0"
          loading="lazy"
          onError={() => setHasError(true)}
        />
      ) : (
        <Landmark className="w-3 h-3 shrink-0" aria-hidden="true" />
      )}
      <span className="whitespace-nowrap">{info.code}</span>
    </span>
  );
};

export const TransactionsTable: React.FC<TransactionsTableProps> = ({
  transactions,
  isLoading,
  onSelectTransaction,
}) => {
  const renderTableBody = () => {
    if (isLoading) {
      // Structured Skeleton Loading (5 rows)
      return Array.from({ length: 5 }).map((_, idx) => (
        <tr key={`skeleton-${idx}`} className="animate-pulse">
          <td className="px-6 py-4 text-left whitespace-nowrap">
            <div className="flex flex-col gap-1">
              <Skeleton className="h-4 w-20 rounded-md" />
              <Skeleton className="h-3 w-12 rounded-md" />
            </div>
          </td>
          <td className="px-6 py-4 text-left">
            <div className="flex flex-col gap-1.5">
              <Skeleton className="h-4 w-44 rounded-md" />
              <Skeleton className="h-3 w-28 rounded-md" />
            </div>
          </td>
          <td className="px-6 py-4 text-left whitespace-nowrap">
            <div className="flex flex-col gap-1.5">
              <Skeleton className="h-5 w-28 rounded-md" />
              <Skeleton className="h-3 w-20 rounded-md" />
            </div>
          </td>
          <td className="px-6 py-4 text-left whitespace-nowrap">
            <Skeleton className="h-6 w-28 rounded-md" />
          </td>
          <td className="px-6 py-4 text-left whitespace-nowrap">
            <Skeleton className="h-5 w-16 rounded-md" />
          </td>
          <td className="px-6 py-4 text-center whitespace-nowrap min-w-[150px]">
            <div className="flex items-center justify-center">
              <Skeleton className="h-5 w-24 rounded-md" />
            </div>
          </td>
          <td className="px-6 py-4 text-center whitespace-nowrap min-w-[80px]">
            <div className="flex items-center justify-center">
              <Skeleton className="h-7 w-7 rounded-lg" />
            </div>
          </td>
        </tr>
      ));
    }

    if (transactions.length === 0) {
      // Rich Empty State
      return (
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
      );
    }

    return transactions.map((t) => (
      <tr
        key={t.id}
        className="hover:bg-brand-light/20 transition-colors duration-150 group"
      >
        {/* Data e Hora - Alinhadas à esquerda */}
        <td className="px-6 py-4 text-left whitespace-nowrap">
          <div className="flex flex-col">
            <span className="text-slate-700 font-semibold tabular-nums">
              {formatDateBR(t.transactionDateUtc)}
            </span>
            <span className="text-[10px] text-slate-400 font-mono tabular-nums">
              {formatTimeBR(t.transactionDateUtc)}
            </span>
          </div>
        </td>

        {/* Descrição - Alinhada à esquerda */}
        <td className="px-6 py-4 text-left">
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

        {/* Instituição e Conta - Alinhada à esquerda */}
        <td className="px-6 py-4 text-left whitespace-nowrap">
          <div className="flex flex-col items-start gap-1">
            <BankLogoTag institutionId={t.institutionId} />
            <span className="text-[10px] text-slate-400 font-mono whitespace-nowrap pl-0.5">
              Conta {maskSensitiveAccount(t.accountNumber)}
            </span>
          </div>
        </td>

        {/* Categoria - Alinhada à esquerda */}
        <td className="px-6 py-4 text-left whitespace-nowrap">
          <CategoryTagPopover
            transactionId={t.id}
            currentCategoryId={t.categoryId}
          />
        </td>

        {/* Meio de Pagamento - Alinhado à esquerda */}
        <td className="px-6 py-4 text-left whitespace-nowrap">
          <span className="inline-flex items-center px-2 py-0.5 rounded-md bg-surface-ground border border-border-subtle text-[11px] font-mono text-slate-600 whitespace-nowrap">
            {formatPaymentMethod(t.channel)}
          </span>
        </td>

        {/* Valor - Centralizado na célula com largura protegida e sem quebra */}
        <td className="px-6 py-4 text-center whitespace-nowrap min-w-[150px]">
          <div className="flex items-center justify-center">
            <span
              className={cn(
                'tabular-nums tracking-tight font-mono inline-flex items-center justify-center gap-1 font-bold text-sm whitespace-nowrap',
                t.type === 'Credit'
                  ? 'text-status-success'
                  : 'text-status-danger'
              )}
            >
              {t.type === 'Credit' ? (
                <ArrowUpRight className="w-4 h-4 shrink-0" aria-hidden="true" />
              ) : (
                <ArrowDownRight className="w-4 h-4 shrink-0" aria-hidden="true" />
              )}
              <span className="whitespace-nowrap">
                {t.type === 'Credit' ? '+ ' : '- '}
                {formatCurrencyBRL(t.amount)}
              </span>
            </span>
          </div>
        </td>

        {/* Ações - Centralizado */}
        <td className="px-6 py-4 text-center whitespace-nowrap min-w-[80px]">
          <div className="flex items-center justify-center">
            <button
              type="button"
              onClick={() => onSelectTransaction(t)}
              aria-label={`Ver detalhes da transação ${t.description}`}
              title="Ver Detalhes"
              className="p-2 text-slate-400 hover:text-brand hover:bg-brand-light rounded-lg transition-all duration-200 cursor-pointer"
            >
              <Eye className="w-4 h-4" />
            </button>
          </div>
        </td>
      </tr>
    ));
  };

  return (
    <Card className="p-0 overflow-hidden bg-surface-card border border-border-subtle shadow-card" hoverable={false}>
      <div className="overflow-x-auto">
        <table className="w-full text-left text-xs">
          <thead>
            <tr className="bg-secondary text-white font-semibold uppercase tracking-wider text-[11px]">
              <th className="px-6 py-4 text-left whitespace-nowrap">Data</th>
              <th className="px-6 py-4 text-left">Descrição e Estabelecimento</th>
              <th className="px-6 py-4 text-left whitespace-nowrap">Instituição e Conta</th>
              <th className="px-6 py-4 text-left whitespace-nowrap">Categoria</th>
              <th className="px-6 py-4 text-left whitespace-nowrap">Meio</th>
              <th className="px-6 py-4 text-left whitespace-nowrap">Valor</th>
              <th className="px-6 py-4 text-left whitespace-nowrap">Ações</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border-subtle bg-surface-card">
            {renderTableBody()}
          </tbody>
        </table>
      </div>
    </Card>
  );
};
