import React, { lazy, Suspense } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AppLayout } from '@/app/layout/AppLayout';
import { Skeleton } from '@/shared/components/Skeleton/Skeleton';

const LoginPage = lazy(() => import('@/features/auth/pages/LoginPage'));
const DashboardPage = lazy(() => import('@/features/dashboard/pages/DashboardPage'));
const TransactionsPage = lazy(() => import('@/features/transactions/pages/TransactionsPage'));
const ConnectionsPage = lazy(() => import('@/features/connections/pages/ConnectionsPage'));

export const AppRoutes: React.FC = () => {
  return (
    <BrowserRouter>
      <Routes>
        {/* LoginPage é lazy e fica fora do AppLayout, que é quem provê o Suspense das
            demais rotas. Sem esta fronteira, a navegação a frio para /login suspende sem
            fallback e o React 19 lança. */}
        <Route
          path="/login"
          element={
            <Suspense fallback={<Skeleton className="w-full h-screen" />}>
              <LoginPage />
            </Suspense>
          }
        />

        <Route path="/" element={<AppLayout />}>
          <Route index element={<DashboardPage />} />
          <Route path="transacoes" element={<TransactionsPage />} />
          <Route path="conexoes" element={<ConnectionsPage />} />
        </Route>

        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  );
};
