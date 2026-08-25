import React from 'react';
import { Card } from '@/shared/components/Card/Card';
import { Skeleton } from '@/shared/components/Skeleton/Skeleton';

export const DashboardSkeleton: React.FC = () => {
  return (
    <div className="flex flex-col gap-6" aria-busy="true" aria-label="Carregando visão geral do painel">
      {/* 3 KPI Card Skeletons */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        {[1, 2, 3].map((i) => (
          <Card key={i} className="flex flex-col gap-3 p-6 bg-surface-card border border-border-subtle" hoverable={false}>
            <div className="flex items-center justify-between">
              <Skeleton className="h-4 w-32 rounded-md" />
              <Skeleton className="h-10 w-10 rounded-full" />
            </div>
            <Skeleton className="h-8 w-44 rounded-lg" />
            <Skeleton className="h-3 w-28 rounded-md" />
          </Card>
        ))}
      </div>

      {/* Main Grid Skeletons (Bank Accounts & Donut Chart) */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <Card className="lg:col-span-2 flex flex-col p-6 bg-surface-card border border-border-subtle" hoverable={false}>
          <div className="flex items-center justify-between mb-6">
            <Skeleton className="h-5 w-40 rounded-md" />
            <Skeleton className="h-4 w-20 rounded-md" />
          </div>
          <div className="flex flex-col gap-3">
            {[1, 2, 3].map((i) => (
              <div key={i} className="flex items-center justify-between p-4 bg-surface-ground rounded-xl border border-border-subtle">
                <div className="flex items-center gap-3">
                  <Skeleton className="h-10 w-10 rounded-full" />
                  <div className="flex flex-col gap-1.5">
                    <Skeleton className="h-4 w-28 rounded-md" />
                    <Skeleton className="h-3 w-36 rounded-md" />
                  </div>
                </div>
                <Skeleton className="h-5 w-24 rounded-md" />
              </div>
            ))}
          </div>
        </Card>

        <Card className="flex flex-col p-6 items-center justify-between bg-surface-card border border-border-subtle" hoverable={false}>
          <div className="w-full flex items-center justify-between mb-4">
            <Skeleton className="h-5 w-36 rounded-md" />
          </div>
          <Skeleton className="h-44 w-44 rounded-full my-4" />
          <div className="w-full grid grid-cols-2 gap-2 mt-2">
            {[1, 2, 3, 4].map((i) => (
              <Skeleton key={i} className="h-4 w-full rounded-md" />
            ))}
          </div>
        </Card>
      </div>
    </div>
  );
};
