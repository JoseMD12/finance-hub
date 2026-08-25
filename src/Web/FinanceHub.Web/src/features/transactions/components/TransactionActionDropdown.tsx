import React from 'react';
import { DropdownMenu, type DropdownMenuItem } from '@/shared/components/DropdownMenu/DropdownMenu';
import { MoreVertical, Receipt, ArrowLeftRight, Check, Eye } from 'lucide-react';
import type { TransactionDto } from '../types/transactions.types';

export interface TransactionActionDropdownProps {
  transaction: TransactionDto;
  onSelectTransaction: (transaction: TransactionDto) => void;
  onToggleNeutrality: (transaction: TransactionDto) => void;
  onToggleBillPayment: (transaction: TransactionDto) => void;
}

export const TransactionActionDropdown: React.FC<TransactionActionDropdownProps> = ({
  transaction,
  onSelectTransaction,
  onToggleNeutrality,
  onToggleBillPayment,
}) => {
  const items: DropdownMenuItem[] = [
    {
      key: 'details',
      label: 'Ver Detalhes',
      icon: <Eye className="w-4 h-4 text-slate-500" />,
      onClick: () => onSelectTransaction(transaction),
    },
    {
      key: 'neutrality',
      label: transaction.isIgnoredInTotals ? 'Considerar nos Totais' : 'Ignorar / Neutro',
      icon: transaction.isIgnoredInTotals ? (
        <Check className="w-4 h-4 text-emerald-600" />
      ) : (
        <ArrowLeftRight className="w-4 h-4 text-slate-500" />
      ),
      checked: Boolean(transaction.isIgnoredInTotals),
      onClick: () => onToggleNeutrality(transaction),
    },
    {
      key: 'billPayment',
      label: transaction.isBillPayment ? 'Desmarcar Fatura' : 'Marcar como Fatura',
      icon: <Receipt className="w-4 h-4 text-blue-600 shrink-0" />,
      variant: transaction.isBillPayment ? 'brand' : 'default',
      checked: Boolean(transaction.isBillPayment),
      onClick: () => onToggleBillPayment(transaction),
    },
  ];

  return (
    <DropdownMenu
      align="right"
      trigger={
        <button
          type="button"
          aria-label={`Ações da transação ${transaction.description}`}
          title="Ações"
          className="p-2 text-slate-400 hover:text-brand hover:bg-brand-light rounded-xl transition-all duration-150 cursor-pointer border border-transparent hover:border-brand/20 active:scale-95"
        >
          <MoreVertical className="w-4 h-4" />
        </button>
      }
      items={items}
    />
  );
};
