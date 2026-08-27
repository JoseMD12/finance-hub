import React from 'react';
import { Card } from '@/shared/components/Card/Card';
import { Skeleton } from '@/shared/components/Skeleton/Skeleton';

/**
 * Espelha o layout real do painel — quatro KPIs, duas colunas e a faixa inferior — para que a
 * transição do carregamento para os dados não desloque nada na tela.
 */
export const DashboardSkeleton: React.FC = () => (
  <div className="flex flex-col gap-6" aria-busy="true" aria-label="Carregando visão geral do painel">
    <Skeleton className="h-16 w-full rounded-2xl" />

    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
      {[1, 2, 3, 4].map((index) => (
        <Card
          key={index}
          className="p-4 flex flex-col justify-between gap-3 bg-surface-card border border-border-subtle"
          hoverable={false}
        >
          <div className="flex items-center justify-between">
            <Skeleton className="h-3.5 w-24 rounded-md" />
            <Skeleton className="h-9 w-9 rounded-xl" />
          </div>
          <Skeleton className="h-6 w-32 rounded-lg" />
          <Skeleton className="h-3 w-20 rounded-md" />
        </Card>
      ))}
    </div>

    <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
      {[1, 2].map((index) => (
        <Card
          key={index}
          className="flex flex-col gap-4 bg-surface-card border border-border-subtle"
          hoverable={false}
        >
          <Skeleton className="h-5 w-40 rounded-md" />
          <div className="flex flex-col gap-3">
            {[1, 2, 3, 4].map((row) => (
              <Skeleton key={row} className="h-12 w-full rounded-xl" />
            ))}
          </div>
        </Card>
      ))}
    </div>

    <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
      <Card className="flex flex-col gap-4 bg-surface-card border border-border-subtle" hoverable={false}>
        <Skeleton className="h-5 w-36 rounded-md" />
        <Skeleton className="h-64 w-full rounded-xl" />
      </Card>
      <Card className="flex flex-col gap-4 bg-surface-card border border-border-subtle" hoverable={false}>
        <Skeleton className="h-5 w-44 rounded-md" />
        <div className="flex flex-col gap-3">
          {[1, 2, 3, 4, 5].map((row) => (
            <Skeleton key={row} className="h-10 w-full rounded-xl" />
          ))}
        </div>
      </Card>
    </div>
  </div>
);
