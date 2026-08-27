import React from 'react';
import { Card } from '@/shared/components/Card/Card';
import { formatCurrencyBRL } from '@/shared/utils/formatters';
import { cn } from '@/shared/utils/cn';
import type { CategoryExpenseDto } from '../types/dashboard.types';

export interface CategoryRankedBarsProps {
  categories: readonly CategoryExpenseDto[];
}

/**
 * Despesas por categoria como barras horizontais ranqueadas.
 *
 * Substitui o donut anterior: com oito ou mais fatias ninguém compara ângulos, enquanto a barra
 * ordenada responde "meu maior gasto foi X" num olhar. A largura codifica a grandeza e a ordem
 * codifica o ranking — duas leituras que o donut não entrega.
 *
 * A cor vem do token da categoria (`colorToken`), nunca de hexadecimal inline (Regra 1).
 */

/** Mapeia o token da categoria para a classe de fundo da barra. */
const COLOR_TOKEN_CLASS: Record<string, string> = {
  emerald: 'bg-emerald-500',
  sky: 'bg-sky-500',
  amber: 'bg-amber-500',
  rose: 'bg-rose-500',
  violet: 'bg-violet-500',
  orange: 'bg-orange-500',
  teal: 'bg-teal-500',
  indigo: 'bg-indigo-500',
  slate: 'bg-slate-400',
};

const FALLBACK_BAR_CLASS = 'bg-brand';

function getBarClass(colorToken: string): string {
  return COLOR_TOKEN_CLASS[colorToken] ?? FALLBACK_BAR_CLASS;
}

const CategoryRankedBarsComponent: React.FC<CategoryRankedBarsProps> = ({ categories }) => {
  // A barra é dimensionada contra a maior categoria, não contra o total: assim a diferença
  // entre a primeira e a segunda fica legível mesmo quando a cauda é longa.
  const largestAmount = React.useMemo(
    () => categories.reduce((max, category) => Math.max(max, category.amountBrl), 0),
    [categories],
  );

  return (
    <Card className="flex flex-col" hoverable={false}>
      <h2 className="text-base font-bold text-secondary mb-4">Despesas por Categoria</h2>

      {categories.length === 0 ? (
        <div className="p-6 text-center text-xs text-slate-400 border border-dashed border-border-subtle rounded-xl">
          Sem despesas categorizadas no período.
        </div>
      ) : (
        <ul className="flex flex-col gap-3.5">
          {categories.map((category) => {
            const width = largestAmount > 0 ? (category.amountBrl / largestAmount) * 100 : 0;

            return (
              <li key={category.categoryId || category.categoryName} className="flex flex-col gap-1.5">
                <div className="flex items-baseline justify-between gap-3">
                  <span className="text-xs font-semibold text-slate-700 truncate">
                    {category.categoryName}
                  </span>
                  <span className="text-xs font-bold text-slate-800 tabular-nums shrink-0">
                    {formatCurrencyBRL(category.amountBrl)}
                    <span className="ml-1.5 text-[11px] font-medium text-slate-400">
                      {category.percentage.toFixed(1)}%
                    </span>
                  </span>
                </div>

                <div
                  className="h-2 bg-slate-100 rounded-full overflow-hidden"
                  role="progressbar"
                  aria-valuenow={Math.round(category.percentage)}
                  aria-valuemin={0}
                  aria-valuemax={100}
                  aria-label={`${category.categoryName}: ${formatCurrencyBRL(category.amountBrl)}`}
                >
                  <div
                    className={cn(
                      'h-full rounded-full transition-[width] duration-500',
                      getBarClass(category.colorToken),
                    )}
                    style={{ width: `${width}%` }}
                  />
                </div>
              </li>
            );
          })}
        </ul>
      )}
    </Card>
  );
};

export const CategoryRankedBars = React.memo(CategoryRankedBarsComponent);
