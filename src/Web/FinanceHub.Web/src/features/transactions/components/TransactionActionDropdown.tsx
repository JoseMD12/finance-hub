import React, { useMemo } from 'react';
import { DropdownMenu, type DropdownMenuItem } from '@/shared/components/DropdownMenu/DropdownMenu';
import { MoreVertical, Receipt, ArrowLeftRight, Check } from 'lucide-react';
import type { TransactionDto } from '../types/transactions.types';

export interface TransactionActionDropdownProps {
  transaction: TransactionDto;
  onToggleNeutrality: (transaction: TransactionDto) => void;
  onToggleBillPayment: (transaction: TransactionDto) => void;
}

const TransactionActionDropdownComponent: React.FC<TransactionActionDropdownProps> = ({
  transaction,
  onToggleNeutrality,
  onToggleBillPayment,
}) => {
  const isIgnored = Boolean(transaction.isIgnoredInTotals);
  const isBill = Boolean(transaction.isBillPayment);

  const items: DropdownMenuItem[] = useMemo(() => [
    {
      key: 'neutrality',
      label: isIgnored ? 'Considerar nos Totais' : 'Ignorar / Neutro',
      icon: isIgnored ? (
        <Check className="w-4 h-4 text-emerald-600" />
      ) : (
        <ArrowLeftRight className="w-4 h-4 text-slate-500" />
      ),
      checked: isIgnored,
      onClick: () => onToggleNeutrality(transaction),
    },
    {
      key: 'billPayment',
      label: isBill ? 'Desmarcar Fatura' : 'Marcar como Fatura',
      icon: <Receipt className="w-4 h-4 text-blue-600 shrink-0" />,
      variant: isBill ? 'brand' : 'default',
      checked: isBill,
      onClick: () => onToggleBillPayment(transaction),
    },
  ], [isIgnored, isBill, onToggleNeutrality, onToggleBillPayment, transaction]);

  return (
    <DropdownMenu
      align="right"
      trigger={
        <button
          type="button"
          aria-label={`Ações da transação ${transaction.description}`}
          title="Ações"
          className="w-8 h-8 p-2 text-slate-400 hover:text-secondary hover:bg-secondary-light rounded-xl transition-colors duration-150 cursor-pointer border border-transparent hover:border-secondary/20 active:scale-95 flex items-center justify-center shrink-0"
        >
          <MoreVertical className="w-4 h-4 shrink-0" />
        </button>
      }
      items={items}
    />
  );
};

export const TransactionActionDropdown = React.memo(TransactionActionDropdownComponent);

