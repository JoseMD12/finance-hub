import React from 'react';
import { DropdownMenu, type DropdownMenuItem } from '@/shared/components/DropdownMenu/DropdownMenu';
import { MoreVertical, Receipt, ArrowLeftRight, Check } from 'lucide-react';
import type { TransactionDto } from '../types/transactions.types';

export interface TransactionActionDropdownProps {
  transaction: TransactionDto;
  onToggleNeutrality: (transaction: TransactionDto) => void;
  onToggleBillPayment: (transaction: TransactionDto) => void;
}

export const TransactionActionDropdown: React.FC<TransactionActionDropdownProps> = ({
  transaction,
  onToggleNeutrality,
  onToggleBillPayment,
}) => {
  const items: DropdownMenuItem[] = [
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
          className="w-8 h-8 p-2 text-slate-400 hover:text-secondary hover:bg-secondary-light rounded-xl transition-all duration-150 cursor-pointer border border-transparent hover:border-secondary/20 active:scale-95 flex items-center justify-center shrink-0"
        >
          <MoreVertical className="w-4 h-4 shrink-0" />
        </button>
      }
      items={items}
    />
  );
};
