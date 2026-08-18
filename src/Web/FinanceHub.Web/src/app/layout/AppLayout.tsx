import React, { Suspense } from 'react';
import { Outlet } from 'react-router-dom';
import { Sidebar } from './Sidebar';
import { Topbar } from './Topbar';
import { Skeleton } from '@/shared/components/Skeleton/Skeleton';

export const AppLayout: React.FC = () => {
  return (
    <div className="flex min-h-screen bg-surface-ground">
      <Sidebar />
      <div className="flex-1 flex flex-col min-w-0">
        <Topbar />
        <main className="flex-1 p-8 overflow-y-auto">
          <Suspense fallback={<Skeleton className="w-full h-96" />}>
            <Outlet />
          </Suspense>
        </main>
      </div>
    </div>
  );
};
