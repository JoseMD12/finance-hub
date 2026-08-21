import React from 'react';
import { Card } from '@/shared/components/Card/Card';
import { formatCurrencyBRL, formatDateBR, maskSensitiveAccount } from '@/shared/utils/formatters';
import { Landmark, ArrowUpRight, ArrowDownRight, Eye } from 'lucide-react';
import { CategoryTagPopover } from './CategoryTagPopover';
import type { TransactionDto } from '../types/transactions.types';

export interface TransactionsTableProps {
  transactions: TransactionDto[];
  isLoading: boolean;
  onSelectTransaction: (transaction: TransactionDto) => void;
}

export const TransactionsTable: React.FC<TransactionsTableProps> = ({
  transactions,
  isLoading,
  onSelectTransaction,
}) => {
  return (
    <Card className="p-0 overflow-hidden bg-surface-card" hoverable={false}>
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
              <tr>
                <td colSpan={7} className="px-6 py-12 text-center text-slate-400 font-medium">
                  Carregando transações do ledger canônico...
                </td>
              </tr>
            ) : transactions.length === 0 ? (
              <tr>
                <td colSpan={7} className="px-6 py-12 text-center text-slate-400 font-medium">
                  Nenhuma transação encontrada com os filtros selecionados.
                </td>
              </tr>
            ) : (
              transactions.map((t) => (
                <tr
                  key={t.id}
                  className="hover:bg-slate-50/80 transition-colors duration-150"
                >
                  <td className="px-6 py-4 text-slate-500 font-semibold">
                    {formatDateBR(t.transactionDateUtc)}
                  </td>
                  <td className="px-6 py-4">
                    <div className="flex flex-col">
                      <span className="font-bold text-slate-800">{t.description}</span>
                      {t.merchantName && (
                        <span className="text-[11px] text-slate-400 font-medium">
                          {t.merchantName}
                        </span>
                      )}
                    </div>
                  </td>
                  <td className="px-6 py-4 text-slate-600">
                    <div className="flex flex-col">
                      <span className="inline-flex items-center gap-1.5 font-bold text-slate-800">
                        <Landmark className="w-3.5 h-3.5 text-secondary" />
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
                  <td className="px-6 py-4 text-slate-600 font-medium">{t.channel}</td>
                  <td className="px-6 py-4 text-right font-extrabold text-sm">
                    <span
                      className={
                        t.type === 'Credit'
                          ? 'text-status-success inline-flex items-center gap-1'
                          : 'text-status-danger inline-flex items-center gap-1'
                      }
                    >
                      {t.type === 'Credit' ? (
                        <ArrowUpRight className="w-4 h-4" />
                      ) : (
                        <ArrowDownRight className="w-4 h-4" />
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
                      className="p-1.5 text-slate-400 hover:text-brand hover:bg-brand-light rounded-lg transition-all duration-200 cursor-pointer"
                    >
                      <Eye className="w-4 h-4" />
                    </button>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </Card>
  );
};
