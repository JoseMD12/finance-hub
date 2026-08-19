import { Card } from '@/shared/components/Card/Card';
import { formatCurrencyBRL, formatDateTimeBR } from '@/shared/utils/formatters';
import { CreditCard, Wallet } from 'lucide-react';
import React from 'react';
import type { PluggyItemDto } from '../types/connections.types';
import { InstitutionLogo } from './InstitutionLogo';

interface ConnectionCardProps {
  item: PluggyItemDto;
}

export const ConnectionCard: React.FC<ConnectionCardProps> = ({ item }) => {
  return (
    <Card className="flex flex-col justify-between gap-4 hoverable border-slate-200/80">
      <div className="flex flex-col gap-3">
        <div className="flex items-start justify-between gap-3">
          <div className="flex flex-col items-center gap-1.5 min-w-0 text-center">
            <InstitutionLogo institutionName={item.connector.name} />
            <h3 className="text-xs font-bold leading-tight text-slate-800 break-words text-center">
              {item.connector.name}
            </h3>
          </div>
          <div className="flex flex-col items-end gap-1.5 flex-shrink-0">
            <span className="text-[11px] text-slate-400 font-medium whitespace-nowrap">
              Atualizado: {formatDateTimeBR(item.lastUpdatedAt)}
            </span>
          </div>
        </div>
      </div>

      <div className="pt-3 border-t border-slate-100 flex flex-col gap-2">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-1.5 text-xs text-slate-500 font-medium">
            <Wallet className="w-3.5 h-3.5 text-slate-400" />
            <span>Saldo Total</span>
          </div>
          <span
            className={`text-sm font-bold ${
              item.totalBalance < 0 ? 'text-status-danger' : 'text-slate-800'
            }`}
          >
            {formatCurrencyBRL(item.totalBalance)}
          </span>
        </div>
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-1.5 text-xs text-slate-500 font-medium">
            <CreditCard className="w-3.5 h-3.5 text-slate-400" />
            <span>Crédito Total</span>
          </div>
          <span className="text-sm font-bold text-slate-800">
            {formatCurrencyBRL(item.totalCredit)}
          </span>
        </div>
      </div>
    </Card>
  );
};
