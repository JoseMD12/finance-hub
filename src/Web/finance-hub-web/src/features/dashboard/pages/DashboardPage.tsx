import React from 'react';
import { Card } from '@/shared/components/Card/Card';
import { formatCurrencyBRL } from '@/shared/utils/formatters';
import { Landmark, TrendingUp, TrendingDown } from 'lucide-react';
import { PieChart, Pie, Cell, ResponsiveContainer, Tooltip } from 'recharts';

export const DashboardPage: React.FC = () => {
  const categoryData = [
    { name: 'Alimentação', value: 2000, color: '#E05697' },
    { name: 'Trabalho', value: 1500, color: '#1D555A' },
    { name: 'Transporte', value: 800, color: '#FF7338' },
    { name: 'Lazer', value: 500, color: '#38BDF8' },
  ];

  const banks = [
    { name: 'Itaú Unibanco', balance: 14500.90, badge: 'Open Finance' },
    { name: 'Banco Inter', balance: 6850.00, badge: 'Conta Digital' },
    { name: 'Mercado Pago', balance: 3500.00, badge: 'Carteira' },
  ];

  return (
    <div className="flex flex-col gap-8">
      {/* Top Metrics Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <Card className="flex flex-col gap-2">
          <div className="flex items-center justify-between">
            <span className="text-xs font-semibold text-slate-500">Saldo Consolidado</span>
            <Landmark className="w-4 h-4 text-brand" />
          </div>
          <div className="text-2xl font-extrabold text-brand tracking-tight">
            {formatCurrencyBRL(24850.90)}
          </div>
          <span className="text-[11px] text-slate-400 font-medium">Multi-bancos ativos</span>
        </Card>

        <Card className="flex flex-col gap-2">
          <div className="flex items-center justify-between">
            <span className="text-xs font-semibold text-slate-500">Receitas do Mês</span>
            <TrendingUp className="w-4 h-4 text-status-success" />
          </div>
          <div className="text-2xl font-extrabold text-status-success tracking-tight">
            + {formatCurrencyBRL(8450.00)}
          </div>
          <span className="text-[11px] text-slate-400 font-medium">3 transferências Pix / Salário</span>
        </Card>

        <Card className="flex flex-col gap-2">
          <div className="flex items-center justify-between">
            <span className="text-xs font-semibold text-slate-500">Despesas do Mês</span>
            <TrendingDown className="w-4 h-4 text-status-danger" />
          </div>
          <div className="text-2xl font-extrabold text-status-danger tracking-tight">
            - {formatCurrencyBRL(4800.00)}
          </div>
          <span className="text-[11px] text-slate-400 font-medium">60% do teto planejado</span>
        </Card>
      </div>

      {/* Main Grid: Banks & Category Donut */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Connected Banks Card */}
        <Card className="lg:col-span-2 flex flex-col justify-between">
          <div>
            <h2 className="text-base font-bold text-secondary mb-4">Instituições Conectadas</h2>
            <div className="flex flex-col gap-3">
              {banks.map((bank) => (
                <div
                  key={bank.name}
                  className="flex items-center justify-between p-4 bg-surface-ground rounded-xl border border-border-subtle"
                >
                  <div className="flex items-center gap-3">
                    <div className="w-8 h-8 rounded-lg bg-secondary/10 text-secondary flex items-center justify-center font-bold text-xs">
                      {bank.name.substring(0, 2).toUpperCase()}
                    </div>
                    <div>
                      <span className="text-sm font-bold text-slate-800">{bank.name}</span>
                      <span className="ml-2 text-[10px] font-bold px-2 py-0.5 rounded-full bg-slate-200 text-slate-600">
                        {bank.badge}
                      </span>
                    </div>
                  </div>
                  <span className="text-sm font-extrabold text-slate-800">
                    {formatCurrencyBRL(bank.balance)}
                  </span>
                </div>
              ))}
            </div>
          </div>
        </Card>

        {/* Category Expense Donut */}
        <Card className="flex flex-col items-center justify-between">
          <h2 className="text-base font-bold text-secondary w-full text-left mb-2">Despesas por Categoria</h2>
          <div className="w-full h-48">
            <ResponsiveContainer width="100%" height="100%">
              <PieChart>
                <Pie
                  data={categoryData}
                  cx="50%"
                  cy="50%"
                  innerRadius={50}
                  outerRadius={75}
                  paddingAngle={4}
                  dataKey="value"
                >
                  {categoryData.map((entry) => (
                    <Cell key={`cell-${entry.name}`} fill={entry.color} />
                  ))}
                </Pie>
                <Tooltip
                  formatter={(val: any) => [formatCurrencyBRL(Number(val)), 'Gasto']}
                  contentStyle={{ borderRadius: '12px', border: '1px solid #E2E8F0', fontSize: '12px' }}
                />
              </PieChart>
            </ResponsiveContainer>
          </div>
          <div className="w-full grid grid-cols-2 gap-2 text-xs text-slate-600 mt-2">
            {categoryData.map((c) => (
              <div key={c.name} className="flex items-center gap-1.5 truncate">
                <span className="w-2.5 h-2.5 rounded-full flex-shrink-0" style={{ backgroundColor: c.color }} />
                <span className="truncate">{c.name}</span>
              </div>
            ))}
          </div>
        </Card>
      </div>
    </div>
  );
};

export default DashboardPage;
