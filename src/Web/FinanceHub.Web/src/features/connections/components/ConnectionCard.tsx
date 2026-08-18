import React from 'react';
import { Card } from '@/shared/components/Card/Card';
import { Landmark, ShieldCheck, Wallet } from 'lucide-react';
import { formatCurrencyBRL } from '@/shared/utils/formatters';
import type { AccountBalanceDto } from '@/features/dashboard/types/dashboard.types';

interface ConnectionCardProps {
  account: AccountBalanceDto;
}

export const ConnectionCard: React.FC<ConnectionCardProps> = ({ account }) => {
  return (
    <Card className="flex flex-col justify-between gap-5 hoverable border-slate-200/80">
      <div className="flex flex-col gap-3">
        <div className="flex items-center justify-between">
          <div className="w-10 h-10 rounded-xl bg-secondary-light text-secondary flex items-center justify-center font-bold text-sm shadow-sm">
            <Landmark className="w-5 h-5" />
          </div>
          <span className="inline-flex items-center gap-1 text-[11px] font-bold px-2.5 py-1 rounded-full bg-status-success-bg text-status-success">
            <ShieldCheck className="w-3.5 h-3.5" />
            Conectado
          </span>
        </div>

        <div>
          <h3 className="text-sm font-bold text-slate-800">{account.institutionName}</h3>
          <span className="text-[11px] text-slate-400 font-medium">
            {account.badge || 'Meu.Pluggy Open Finance'} • Conta {account.accountNumber}
          </span>
        </div>
      </div>

      <div className="pt-3 border-t border-slate-100 flex items-center justify-between">
        <div className="flex items-center gap-1.5 text-slate-500">
          <Wallet className="w-3.5 h-3.5 text-secondary" />
          <span className="text-[11px] font-medium">Saldo Atual</span>
        </div>
        <span className="text-xs font-bold text-slate-800">
          {formatCurrencyBRL(account.balanceBrl)}
        </span>
      </div>
    </Card>
  );
};
