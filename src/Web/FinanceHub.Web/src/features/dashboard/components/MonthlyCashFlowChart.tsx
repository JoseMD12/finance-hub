import React from 'react';
import {
  Bar,
  BarChart,
  CartesianGrid,
  Legend,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';
import { Card } from '@/shared/components/Card/Card';
import { formatCurrencyBRL } from '@/shared/utils/formatters';
import type { MonthlyCashFlowPointDto } from '../types/dashboard.types';

export interface MonthlyCashFlowChartProps {
  points: readonly MonthlyCashFlowPointDto[];
}

const MONTH_LABELS = [
  'jan', 'fev', 'mar', 'abr', 'mai', 'jun',
  'jul', 'ago', 'set', 'out', 'nov', 'dez',
] as const;

interface ChartPoint {
  readonly label: string;
  readonly Entradas: number;
  readonly Saídas: number;
  readonly net: number;
}

/** Formatação compacta do eixo: "R$ 8,5k" em vez de "R$ 8.500,00", que não cabe. */
function formatAxisCurrency(value: number): string {
  if (Math.abs(value) >= 1000) {
    return `R$ ${(value / 1000).toFixed(1).replace('.', ',')}k`;
  }
  return `R$ ${value}`;
}

const MonthlyCashFlowChartComponent: React.FC<MonthlyCashFlowChartProps> = ({ points }) => {
  const data = React.useMemo<ChartPoint[]>(
    () =>
      points.map((point) => ({
        label: `${MONTH_LABELS[point.month - 1]}/${String(point.year).slice(-2)}`,
        Entradas: point.incomeBrl,
        Saídas: point.expenseBrl,
        net: point.netBrl,
      })),
    [points],
  );

  return (
    <Card className="flex flex-col" hoverable={false}>
      <div className="flex items-baseline justify-between gap-3 mb-4">
        <h2 className="text-base font-bold text-secondary">Evolução Mensal</h2>
        <span className="text-[11px] text-slate-400 font-medium">Últimos 6 meses</span>
      </div>

      {data.length === 0 ? (
        <div className="p-6 text-center text-xs text-slate-400 border border-dashed border-border-subtle rounded-xl">
          Sem histórico suficiente para montar a evolução.
        </div>
      ) : (
        <>
          <div className="w-full h-64">
            <ResponsiveContainer width="100%" height="100%">
              {/* Um eixo só: entradas e saídas compartilham a mesma escala em reais. */}
              <BarChart data={data} margin={{ top: 8, right: 8, left: 8, bottom: 0 }} barGap={2}>
                <CartesianGrid strokeDasharray="3 3" stroke="var(--color-border-subtle)" vertical={false} />
                <XAxis
                  dataKey="label"
                  tick={{ fontSize: 11, fill: 'var(--color-slate-500)' }}
                  axisLine={false}
                  tickLine={false}
                />
                <YAxis
                  tickFormatter={formatAxisCurrency}
                  tick={{ fontSize: 11, fill: 'var(--color-slate-400)' }}
                  axisLine={false}
                  tickLine={false}
                  width={64}
                />
                <Tooltip
                  cursor={{ fill: 'var(--color-surface-ground)' }}
                  formatter={(value, name) => [formatCurrencyBRL(Number(value)), String(name)]}
                  contentStyle={{
                    borderRadius: '12px',
                    border: '1px solid var(--color-border-subtle)',
                    fontSize: '12px',
                    boxShadow: 'var(--shadow-dropdown)',
                    backgroundColor: 'var(--color-surface-card)',
                  }}
                />
                {/* Legenda sempre presente com duas séries: identidade nunca só por cor. */}
                <Legend
                  wrapperStyle={{ fontSize: '11px', fontWeight: 600, paddingTop: '8px' }}
                  iconType="circle"
                  iconSize={8}
                />
                <Bar dataKey="Entradas" fill="var(--color-chart-income)" radius={[4, 4, 0, 0]} maxBarSize={28} />
                <Bar dataKey="Saídas" fill="var(--color-chart-expense)" radius={[4, 4, 0, 0]} maxBarSize={28} />
              </BarChart>
            </ResponsiveContainer>
          </div>

          {/* Alternativa em tabela: o gráfico não pode ser a única forma de ler o dado. */}
          <details className="mt-3 group">
            <summary className="text-[11px] font-semibold text-slate-500 cursor-pointer hover:text-brand transition-colors duration-200 list-none">
              Ver como tabela
            </summary>
            <div className="mt-2 overflow-x-auto">
              <table className="w-full text-[11px]">
                <thead>
                  <tr className="text-slate-500 border-b border-border-subtle">
                    <th scope="col" className="text-left font-semibold py-1.5">Mês</th>
                    <th scope="col" className="text-right font-semibold py-1.5">Entradas</th>
                    <th scope="col" className="text-right font-semibold py-1.5">Saídas</th>
                    <th scope="col" className="text-right font-semibold py-1.5">Resultado</th>
                  </tr>
                </thead>
                <tbody>
                  {data.map((point) => (
                    <tr key={point.label} className="border-b border-slate-100/80 last:border-0">
                      <td className="py-1.5 font-medium text-slate-600">{point.label}</td>
                      <td className="py-1.5 text-right tabular-nums text-chart-income">
                        {formatCurrencyBRL(point.Entradas)}
                      </td>
                      <td className="py-1.5 text-right tabular-nums text-chart-expense">
                        {formatCurrencyBRL(point['Saídas'])}
                      </td>
                      <td className="py-1.5 text-right tabular-nums font-semibold text-slate-700">
                        {formatCurrencyBRL(point.net)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </details>
        </>
      )}
    </Card>
  );
};

export const MonthlyCashFlowChart = React.memo(MonthlyCashFlowChartComponent);
