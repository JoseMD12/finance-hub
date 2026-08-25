import { formatCurrencyBRL } from '@/shared/utils/formatters';
import { CreditCard, Wallet } from 'lucide-react';
import React from 'react';
import { InstitutionLogo } from './InstitutionLogo';
import { GlowCard } from '@/shared/components/motion';

interface SavedInstitutionCardProps {
  institutionName: string;
  totalBalance: number;
  totalCredit: number;
}

export const SavedInstitutionCard: React.FC<SavedInstitutionCardProps> = ({
  institutionName,
  totalBalance,
  totalCredit,
}) => {
  return (
    <GlowCard
      glowRgb="224, 86, 151"
      className="flex flex-col justify-between gap-4 border-slate-200/80 hoverable"
    >
      <div className="flex flex-col gap-3">
        <div className="flex items-center gap-3">
          <InstitutionLogo institutionName={institutionName} />
          <h3 className="text-sm font-bold leading-tight text-slate-800 break-words">
            {institutionName}
          </h3>
        </div>
      </div>

      <div className="pt-3 border-t border-slate-100 flex flex-col gap-2">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-1.5 text-xs text-slate-500 font-medium">
            <Wallet className="w-3.5 h-3.5 text-slate-400" />
            <span>Saldo em Conta</span>
          </div>
          <span
            className={`text-sm font-bold tabular-nums ${
              totalBalance < 0 ? 'text-status-danger' : 'text-slate-800'
            }`}
          >
            {formatCurrencyBRL(totalBalance)}
          </span>
        </div>
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-1.5 text-xs text-slate-500 font-medium">
            <CreditCard className="w-3.5 h-3.5 text-slate-400" />
            <span>Fatura Cartão</span>
          </div>
          <span className="text-sm font-bold text-slate-800 tabular-nums">
            {formatCurrencyBRL(totalCredit)}
          </span>
        </div>
      </div>
    </GlowCard>
  );
};
