import React from 'react';
import { CreditCard, Landmark } from 'lucide-react';
import { Card } from '@/shared/components/Card/Card';
import { IconCircle } from '@/shared/components/IconCircle/IconCircle';
import { StatusBadge } from '@/shared/components/StatusBadge/StatusBadge';
import { getInstitutionInfo } from '@/shared/constants/institutions';
import { formatCurrencyBRL, formatDateBR } from '@/shared/utils/formatters';
import { cn } from '@/shared/utils/cn';
import type { InstitutionBalanceDto } from '../types/dashboard.types';

export interface InstitutionBalanceListProps {
  balances: readonly InstitutionBalanceDto[];
}

/** Consumo do limite, preferindo o limite personalizado quando a instituição informa. */
function getUsagePercentage(balance: InstitutionBalanceDto): number | null {
  if (!balance.isCreditCard || !balance.creditLimit || balance.creditLimit <= 0) {
    return null;
  }

  const used = balance.usedCreditLimit ?? Math.abs(balance.balanceBrl);
  return Math.min(100, Math.round((used / balance.creditLimit) * 100));
}

function getUsageToneClass(percentage: number): string {
  if (percentage >= 90) return 'bg-status-danger';
  if (percentage >= 70) return 'bg-status-warning';
  return 'bg-brand';
}

const InstitutionBalanceRow: React.FC<{ balance: InstitutionBalanceDto }> = ({ balance }) => {
  const institution = getInstitutionInfo(balance.institutionId);
  const usage = getUsagePercentage(balance);

  return (
    <div className="flex flex-col gap-2 p-4 bg-surface-ground rounded-xl border border-border-subtle hover:border-slate-300 transition-colors duration-200">
      <div className="flex items-center justify-between gap-3">
        <div className="flex items-center gap-3 min-w-0">
          <IconCircle
            icon={balance.isCreditCard ? CreditCard : Landmark}
            tone={balance.isCreditCard ? 'brand' : 'secondary'}
            size="lg"
          />
          <div className="min-w-0">
            <span className="text-sm font-bold text-slate-800 block truncate">{institution.name}</span>
            <StatusBadge
              icon={balance.isCreditCard ? CreditCard : Landmark}
              tone="secondary"
              className="mt-1 px-2 py-0.5 text-[10px]"
            >
              {balance.isCreditCard ? 'Cartão de Crédito' : 'Conta'}
            </StatusBadge>
          </div>
        </div>

        <div className="text-right shrink-0">
          <span
            className={cn(
              'text-sm font-extrabold tabular-nums block',
              balance.isCreditCard ? 'text-status-danger' : 'text-slate-800',
            )}
          >
            {formatCurrencyBRL(Math.abs(balance.balanceBrl))}
          </span>
          {balance.invoiceDueDateUtc && (
            <span className="text-[11px] text-slate-400 font-medium">
              Vence {formatDateBR(balance.invoiceDueDateUtc)}
            </span>
          )}
        </div>
      </div>

      {usage !== null && (
        <div className="flex items-center gap-2">
          <div
            className="flex-1 h-1.5 bg-slate-200/80 rounded-full overflow-hidden"
            role="progressbar"
            aria-valuenow={usage}
            aria-valuemin={0}
            aria-valuemax={100}
            aria-label={`Limite utilizado em ${institution.name}`}
          >
            <div
              className={cn('h-full rounded-full transition-[width] duration-500', getUsageToneClass(usage))}
              style={{ width: `${usage}%` }}
            />
          </div>
          <span className="text-[11px] font-semibold text-slate-500 tabular-nums shrink-0">
            {usage}% do limite
          </span>
        </div>
      )}
    </div>
  );
};

const InstitutionBalanceListComponent: React.FC<InstitutionBalanceListProps> = ({ balances }) => (
  <Card className="flex flex-col" hoverable={false}>
    <h2 className="text-base font-bold text-secondary mb-4">Saldos por Instituição</h2>

    {balances.length === 0 ? (
      <div className="p-6 text-center text-xs text-slate-400 border border-dashed border-border-subtle rounded-xl">
        Nenhuma conta sincronizada ainda.
      </div>
    ) : (
      <div className="flex flex-col gap-3">
        {balances.map((balance) => (
          <InstitutionBalanceRow
            key={`${balance.institutionId}-${balance.accountNumber}`}
            balance={balance}
          />
        ))}
      </div>
    )}
  </Card>
);

export const InstitutionBalanceList = React.memo(InstitutionBalanceListComponent);
