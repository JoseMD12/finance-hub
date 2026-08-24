import React from 'react';
import { Card } from '@/shared/components/Card/Card';
import { Skeleton } from '@/shared/components/Skeleton/Skeleton';
import { formatCurrencyBRL } from '@/shared/utils/formatters';
import { ArrowUpRight, ArrowDownRight, Wallet, Landmark, CreditCard, Info } from 'lucide-react';
import { motion, useReducedMotion } from 'framer-motion';
import { GlowCard } from '@/shared/components/motion';
import { Tooltip } from '@/shared/components/Tooltip/Tooltip';
import type { TransactionSummaryDto } from '../types/transactions.types';

export interface TransactionsSummaryCardsProps {
  summary?: TransactionSummaryDto;
  isLoading?: boolean;
}

export const TransactionsSummaryCards: React.FC<TransactionsSummaryCardsProps> = ({
  summary,
  isLoading,
}) => {
  const prefersReduced = useReducedMotion();

  if (isLoading) {
    return (
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4" aria-busy="true" aria-label="Carregando resumo financeiro">
        {[1, 2, 3, 4].map((i) => (
          <Card key={i} className="p-4 flex items-center justify-between bg-surface-card border border-border-subtle" hoverable={false}>
            <div className="flex flex-col gap-2.5">
              <Skeleton className="h-3.5 w-24 rounded-md" />
              <Skeleton className="h-6 w-32 rounded-lg" />
              <Skeleton className="h-3 w-20 rounded-md" />
            </div>
            <Skeleton className="w-10 h-10 rounded-xl" />
          </Card>
        ))}
      </div>
    );
  }

  const realBalance = summary?.realConsolidatedBalanceBrl ?? 0;
  const openCreditCards = summary?.totalOpenCreditCardsBrl ?? 0;
  const income = summary?.totalIncome ?? 0;
  const expense = summary?.totalExpense ?? 0;
  const net = summary?.netBalance ?? 0;

  const getMotionProps = (delay: number) => {
    if (prefersReduced) return {};
    return {
      initial: { opacity: 0, y: 12 },
      animate: { opacity: 1, y: 0 },
      transition: { duration: 0.3, delay, ease: [0.4, 0, 0.2, 1] as const },
    };
  };

  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
      {/* 1. Saldo Real Consolidado em Contas */}
      <motion.div {...getMotionProps(0)}>
        <GlowCard
          glowRgb="59, 130, 246"
          className="p-4 flex flex-col justify-between hover:border-slate-300 hover:shadow-elevated transition-all duration-200 h-full"
        >
          <div className="flex items-center justify-between gap-2">
            <div className="flex items-center gap-1.5">
              <span className="text-xs font-semibold text-slate-500">Saldo em Contas</span>
              <Tooltip content="Compras podem demorar até 48h para entrar no seu histórico" position="right">
                <span
                  tabIndex={0}
                  role="button"
                  aria-label="Informações sobre o prazo de sincronização"
                  className="p-0.5 rounded-full text-slate-400 hover:text-brand transition-colors cursor-help focus:outline-none"
                >
                  <Info className="w-3.5 h-3.5" aria-hidden="true" />
                </span>
              </Tooltip>
            </div>
            <div className="w-9 h-9 rounded-xl bg-blue-50 flex items-center justify-center text-blue-600 ring-1 ring-blue-500/20 shrink-0">
              <Landmark className="w-4 h-4" aria-hidden="true" />
            </div>
          </div>

          <div className="my-2">
            <span className="text-xl font-black font-display text-slate-900 tabular-nums tracking-tight block">
              {formatCurrencyBRL(realBalance)}
            </span>
          </div>

          <div className="flex items-center justify-between text-[11px] pt-1.5 border-t border-slate-100/80 text-slate-500">
            <span className="inline-flex items-center gap-1 font-medium text-slate-500">
              <CreditCard className="w-3 h-3 text-slate-400" aria-hidden="true" />
              Faturas:
            </span>
            <span className="font-semibold font-mono text-slate-700">
              {formatCurrencyBRL(openCreditCards)}
            </span>
          </div>
        </GlowCard>
      </motion.div>

      {/* 2. Entradas do Período */}
      <motion.div {...getMotionProps(0.04)}>
        <GlowCard
          glowRgb="46, 204, 113"
          className="p-4 flex flex-col justify-between hover:border-slate-300 hover:shadow-elevated transition-all duration-200 h-full"
        >
          <div className="flex items-center justify-between gap-2">
            <span className="text-xs font-semibold text-slate-500">Entradas do Mês</span>
            <div className="w-9 h-9 rounded-xl bg-status-success-bg flex items-center justify-center text-status-success ring-1 ring-status-success/20 shrink-0">
              <ArrowUpRight className="w-4 h-4" aria-hidden="true" />
            </div>
          </div>

          <div className="my-2">
            <span className="text-xl font-black font-display text-status-success tabular-nums tracking-tight block">
              + {formatCurrencyBRL(income)}
            </span>
          </div>

          <div className="text-[11px] pt-1.5 border-t border-slate-100/80 text-slate-400 font-medium truncate">
            Receitas operacionais
          </div>
        </GlowCard>
      </motion.div>

      {/* 3. Saídas do Período */}
      <motion.div {...getMotionProps(0.08)}>
        <GlowCard
          glowRgb="255, 89, 100"
          className="p-4 flex flex-col justify-between hover:border-slate-300 hover:shadow-elevated transition-all duration-200 h-full"
        >
          <div className="flex items-center justify-between gap-2">
            <span className="text-xs font-semibold text-slate-500">Saídas do Mês</span>
            <div className="w-9 h-9 rounded-xl bg-status-danger-bg flex items-center justify-center text-status-danger ring-1 ring-status-danger/20 shrink-0">
              <ArrowDownRight className="w-4 h-4" aria-hidden="true" />
            </div>
          </div>

          <div className="my-2">
            <span className="text-xl font-black font-display text-status-danger tabular-nums tracking-tight block">
              - {formatCurrencyBRL(expense)}
            </span>
          </div>

          <div className="text-[11px] pt-1.5 border-t border-slate-100/80 text-slate-400 font-medium truncate">
            Despesas de vida
          </div>
        </GlowCard>
      </motion.div>

      {/* 4. Resultado / Economia Líquida */}
      <motion.div {...getMotionProps(0.12)}>
        <GlowCard
          glowRgb={net >= 0 ? "46, 204, 113" : "224, 86, 151"}
          className="p-4 flex flex-col justify-between hover:border-slate-300 hover:shadow-elevated transition-all duration-200 h-full"
        >
          <div className="flex items-center justify-between gap-2">
            <span className="text-xs font-semibold text-slate-500">Resultado do Mês</span>
            <div className={`w-9 h-9 rounded-xl flex items-center justify-center shrink-0 ring-1 ${
              net >= 0 
                ? 'bg-emerald-50 text-emerald-600 ring-emerald-500/20' 
                : 'bg-brand-light text-brand ring-brand/20'
            }`}>
              <Wallet className="w-4 h-4" aria-hidden="true" />
            </div>
          </div>

          <div className="my-2">
            <span
              className={`text-xl font-black font-display tabular-nums tracking-tight block ${
                net >= 0 ? 'text-emerald-700' : 'text-brand-dark'
              }`}
            >
              {net >= 0 ? '+ ' : '- '}
              {formatCurrencyBRL(Math.abs(net))}
            </span>
          </div>

          <div className="text-[11px] pt-1.5 border-t border-slate-100/80 text-slate-500 font-medium truncate">
            {net >= 0 ? 'Superávit no período' : 'Déficit no período'}
          </div>
        </GlowCard>
      </motion.div>
    </div>
  );
};
