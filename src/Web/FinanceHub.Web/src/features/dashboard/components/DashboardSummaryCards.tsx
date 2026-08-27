import React from 'react';
import { ArrowDownRight, ArrowUpRight, CreditCard, Info, Landmark, Wallet } from 'lucide-react';
import { motion, useReducedMotion } from 'framer-motion';
import { GlowCard, NumberScramble } from '@/shared/components/motion';
import { Tooltip } from '@/shared/components/Tooltip/Tooltip';
import { formatCurrencyBRL } from '@/shared/utils/formatters';
import type { DashboardSummaryTotalsDto } from '../types/dashboard.types';

export interface DashboardSummaryCardsProps {
  summary?: DashboardSummaryTotalsDto;
}

const DashboardSummaryCardsComponent: React.FC<DashboardSummaryCardsProps> = ({ summary }) => {
  const prefersReduced = useReducedMotion();

  const realBalance = summary?.realConsolidatedBalanceBrl ?? 0;
  const openCreditCards = summary?.totalOpenCreditCardsBrl ?? 0;
  const income = summary?.totalIncome ?? 0;
  const expense = summary?.totalExpense ?? 0;
  const net = summary?.netBalance ?? 0;

  // Apenas animação de entrada. Gestos do Framer Motion são proibidos em cards que
  // permanecem visíveis durante o scroll (Regra 26) — a elevação no hover vem do CSS.
  const getEntryAnimation = React.useCallback(
    (delay: number) =>
      prefersReduced
        ? {}
        : {
            initial: { opacity: 0, y: 6 },
            animate: { opacity: 1, y: 0 },
            transition: { duration: 0.2, delay, ease: [0.25, 0.1, 0.25, 1] as const },
          },
    [prefersReduced],
  );

  const cardClassName =
    'p-4 flex flex-col justify-between hover:border-slate-300 hover:shadow-elevated transition-shadow duration-200 h-full cursor-default';

  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
      <motion.div {...getEntryAnimation(0)} className="h-full">
        <GlowCard hoverable={false} glowRgb="59, 130, 246" className={cardClassName}>
          <div className="flex items-center justify-between gap-2">
            <div className="flex items-center gap-1.5">
              <span className="text-xs font-semibold text-slate-500">Saldo em Contas</span>
              <Tooltip content="Compras podem demorar até 48h para entrar no seu histórico" position="right">
                <button
                  type="button"
                  aria-label="Informações sobre o prazo de sincronização"
                  className="p-0.5 rounded-full text-slate-400 hover:text-brand transition-colors cursor-help focus:outline-none bg-transparent border-0"
                >
                  <Info className="w-3.5 h-3.5" aria-hidden="true" />
                </button>
              </Tooltip>
            </div>
            <div className="w-9 h-9 rounded-xl bg-blue-50 flex items-center justify-center text-blue-600 ring-1 ring-blue-500/20 shrink-0">
              <Landmark className="w-4 h-4" aria-hidden="true" />
            </div>
          </div>

          <div className="my-2">
            <NumberScramble
              value={realBalance}
              format={formatCurrencyBRL}
              className="text-xl font-black text-slate-900 tracking-tight block"
            />
          </div>

          <div className="flex items-center justify-between text-[11px] pt-1.5 border-t border-slate-100/80 text-slate-500">
            <span className="inline-flex items-center gap-1 font-medium">
              <CreditCard className="w-3 h-3 text-slate-400" aria-hidden="true" />
              Faturas:
            </span>
            <span className="font-semibold font-mono text-slate-700">
              {formatCurrencyBRL(openCreditCards)}
            </span>
          </div>
        </GlowCard>
      </motion.div>

      <motion.div {...getEntryAnimation(0.02)} className="h-full">
        <GlowCard hoverable={false} glowRgb="46, 204, 113" className={cardClassName}>
          <div className="flex items-center justify-between gap-2">
            <span className="text-xs font-semibold text-slate-500">Entradas do Período</span>
            <div className="w-9 h-9 rounded-xl bg-status-success-bg flex items-center justify-center text-status-success ring-1 ring-status-success/20 shrink-0">
              <ArrowUpRight className="w-4 h-4" aria-hidden="true" />
            </div>
          </div>

          <div className="my-2">
            <NumberScramble
              value={income}
              format={(value) => `+ ${formatCurrencyBRL(value)}`}
              className="text-xl font-black text-status-success tracking-tight block"
            />
          </div>

          <div className="text-[11px] pt-1.5 border-t border-slate-100/80 text-slate-400 font-medium truncate">
            Receitas operacionais
          </div>
        </GlowCard>
      </motion.div>

      <motion.div {...getEntryAnimation(0.04)} className="h-full">
        <GlowCard hoverable={false} glowRgb="255, 89, 100" className={cardClassName}>
          <div className="flex items-center justify-between gap-2">
            <span className="text-xs font-semibold text-slate-500">Saídas do Período</span>
            <div className="w-9 h-9 rounded-xl bg-status-danger-bg flex items-center justify-center text-status-danger ring-1 ring-status-danger/20 shrink-0">
              <ArrowDownRight className="w-4 h-4" aria-hidden="true" />
            </div>
          </div>

          <div className="my-2">
            <NumberScramble
              value={expense}
              format={(value) => `- ${formatCurrencyBRL(value)}`}
              className="text-xl font-black text-status-danger tracking-tight block"
            />
          </div>

          <div className="text-[11px] pt-1.5 border-t border-slate-100/80 text-slate-400 font-medium truncate">
            Despesas de vida
          </div>
        </GlowCard>
      </motion.div>

      <motion.div {...getEntryAnimation(0.06)} className="h-full">
        <GlowCard
          hoverable={false}
          glowRgb={net >= 0 ? '46, 204, 113' : '224, 86, 151'}
          className={cardClassName}
        >
          <div className="flex items-center justify-between gap-2">
            <span className="text-xs font-semibold text-slate-500">Resultado do Período</span>
            <div
              className={`w-9 h-9 rounded-xl flex items-center justify-center shrink-0 ring-1 ${
                net >= 0
                  ? 'bg-emerald-50 text-emerald-600 ring-emerald-500/20'
                  : 'bg-brand-light text-brand ring-brand/20'
              }`}
            >
              <Wallet className="w-4 h-4" aria-hidden="true" />
            </div>
          </div>

          <div className="my-2">
            <NumberScramble
              value={net}
              format={(value) => `${value >= 0 ? '+ ' : '- '}${formatCurrencyBRL(Math.abs(value))}`}
              className={`text-xl font-black tracking-tight block ${
                net >= 0 ? 'text-emerald-700' : 'text-brand-dark'
              }`}
            />
          </div>

          <div className="text-[11px] pt-1.5 border-t border-slate-100/80 text-slate-500 font-medium truncate">
            {net >= 0 ? 'Superávit no período' : 'Déficit no período'}
          </div>
        </GlowCard>
      </motion.div>
    </div>
  );
};

export const DashboardSummaryCards = React.memo(DashboardSummaryCardsComponent);
