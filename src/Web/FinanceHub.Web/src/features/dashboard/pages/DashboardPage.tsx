import React from 'react';
import { Card } from '@/shared/components/Card/Card';
import { formatCurrencyBRL } from '@/shared/utils/formatters';
import { Landmark, TrendingUp, TrendingDown, ArrowUpRight, Loader2 } from 'lucide-react';
import { PieChart, Pie, Cell, ResponsiveContainer, Tooltip } from 'recharts';
import { useDashboardQuery } from '../hooks/useDashboardQuery';
import { IconCircle } from '@/shared/components/IconCircle/IconCircle';
import { StatusBadge } from '@/shared/components/StatusBadge/StatusBadge';

export const DashboardPage: React.FC = () => {
  const { data: dashboard, isLoading, error } = useDashboardQuery();

  const totalBalance = dashboard?.totalBalanceBrl ?? 0;
  const monthlyIncome = dashboard?.monthlyIncomeBrl ?? 0;
  const monthlyExpense = dashboard?.monthlyExpenseBrl ?? 0;
  const accountBalances = dashboard?.accountBalances ?? [];
  const categoryExpenses = dashboard?.categoryExpenses ?? [];

  return (
    <div className="flex flex-col gap-8 select-none">
      {/* Dashboard Section Header */}
      <div>
        <h1 className="section-title text-xl font-extrabold text-secondary">
          Visão Geral e Saldos Consolidados
        </h1>
        <p className="text-xs text-slate-500 font-medium mt-1">
          Monitoramento unificado de patrimônio via Open Finance e ingestão de extratos
        </p>
      </div>

      {isLoading && (
        <div className="flex items-center justify-center p-12 text-slate-400 gap-2">
          <Loader2 className="w-5 h-5 animate-spin text-brand" />
          <span className="text-xs font-semibold">Carregando dados do Dashboard...</span>
        </div>
      )}

      {error && (
        <Card className="p-4 border-status-danger/30 bg-status-danger-bg text-status-danger text-xs font-semibold">
          Não foi possível carregar as informações do dashboard no momento.
        </Card>
      )}

      {!isLoading && (
        <>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            <Card className="flex flex-col gap-2">
              <div className="flex items-center justify-between">
                <span className="text-xs font-semibold text-slate-500">Saldo Consolidado Total</span>
                <IconCircle icon={Landmark} tone="brand" size="md" />
              </div>
              <div className="text-2xl font-extrabold text-brand tracking-tight">
                {formatCurrencyBRL(totalBalance)}
              </div>
              <span className="text-[11px] text-slate-400 font-medium">
                {accountBalances.length} instituição(ões) vinculada(s)
              </span>
            </Card>
            <Card className="flex flex-col gap-2">
              <div className="flex items-center justify-between">
                <span className="text-xs font-semibold text-slate-500">Receitas do Mês</span>
                <IconCircle icon={TrendingUp} tone="success" size="md" />
              </div>
              <div className="text-2xl font-extrabold text-status-success tracking-tight">
                + {formatCurrencyBRL(monthlyIncome)}
              </div>
              <span className="text-[11px] text-slate-400 font-medium">Entradas consolidadas</span>
            </Card>
            <Card className="flex flex-col gap-2">
              <div className="flex items-center justify-between">
                <span className="text-xs font-semibold text-slate-500">Despesas do Mês</span>
                <IconCircle icon={TrendingDown} tone="danger" size="md" />
              </div>
              <div className="text-2xl font-extrabold text-status-danger tracking-tight">
                - {formatCurrencyBRL(monthlyExpense)}
              </div>
              <span className="text-[11px] text-slate-400 font-medium">Lançamentos deduplicados no Ledger</span>
            </Card>
          </div>

          {/* Main Grid: Banks & Category Donut */}
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
            {/* Connected Banks Card */}
            <Card className="lg:col-span-2 flex flex-col justify-between" hoverable={false}>
              <div>
                <h2 className="text-base font-bold text-secondary mb-4 flex items-center justify-between">
                  <span>Saldos por Instituição</span>
                  <span className="text-xs font-semibold text-brand hover:underline cursor-pointer flex items-center gap-1">
                    Ver detalhes <ArrowUpRight className="w-3.5 h-3.5" />
                  </span>
                </h2>
                <div className="flex flex-col gap-3">
                  {accountBalances.length === 0 ? (
                    <div className="p-6 text-center text-xs text-slate-400 border border-dashed rounded-xl">
                      Nenhuma conta cadastrada ou sincronizada ainda.
                    </div>
                  ) : (
                    accountBalances.map((bank) => (
                      <div
                        key={bank.institutionName + bank.accountNumber}
                        className="flex items-center justify-between p-4 bg-surface-ground rounded-xl border border-border-subtle hover:border-slate-300 transition-colors"
                      >
                        <div className="flex items-center gap-3">
                          <IconCircle icon={Landmark} tone="secondary" size="lg" />
                          <div>
                            <span className="text-sm font-bold text-slate-800">{bank.institutionName}</span>
                            <StatusBadge icon={Landmark} tone="secondary" className="ml-2.5 px-2 py-0.5 text-[10px]">
                              {bank.badge ?? 'Meu.Pluggy Open Finance'}
                            </StatusBadge>
                          </div>
                        </div>
                        <span className="text-sm font-extrabold text-slate-800">
                          {formatCurrencyBRL(bank.balanceBrl)}
                        </span>
                      </div>
                    ))
                  )}
                </div>
              </div>
            </Card>

            {/* Category Expense Donut */}
            <Card className="flex flex-col items-center justify-between" hoverable={false}>
              <h2 className="text-base font-bold text-secondary w-full text-left mb-2">Despesas por Categoria</h2>
              {categoryExpenses.length === 0 ? (
                <div className="p-6 text-center text-xs text-slate-400 w-full">
                  Sem despesas categorizadas no período.
                </div>
              ) : (
                <>
                  <div className="w-full h-48">
                    <ResponsiveContainer width="100%" height="100%">
                      <PieChart>
                        <Pie
                          data={categoryExpenses}
                          cx="50%"
                          cy="50%"
                          innerRadius={50}
                          outerRadius={75}
                          paddingAngle={4}
                          dataKey="amountBrl"
                          nameKey="categoryName"
                        >
                          {categoryExpenses.map((entry) => (
                            <Cell key={`cell-${entry.categoryName}`} fill={entry.color || 'var(--color-brand)'} />
                          ))}
                        </Pie>
                        <Tooltip
                          formatter={(val: any) => [formatCurrencyBRL(Number(val)), 'Total']}
                          contentStyle={{ borderRadius: '12px', border: '1px solid #E2E8F0', fontSize: '12px', boxShadow: 'var(--shadow-dropdown)' }}
                        />
                      </PieChart>
                    </ResponsiveContainer>
                  </div>
                  <div className="w-full grid grid-cols-2 gap-2 text-xs text-slate-600 mt-2">
                    {categoryExpenses.map((c) => (
                      <div key={c.categoryName} className="flex items-center gap-1.5 truncate">
                        <span className="w-2.5 h-2.5 rounded-full flex-shrink-0" style={{ backgroundColor: c.color || 'var(--color-brand)' }} />
                        <span className="truncate font-medium">{c.categoryName}</span>
                      </div>
                    ))}
                  </div>
                </>
              )}
            </Card>
          </div>
        </>
      )}
    </div>
  );
};

export default DashboardPage;
