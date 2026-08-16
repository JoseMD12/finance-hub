import React, { lazy } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AppLayout } from '@/app/layout/AppLayout';

const LoginPage = lazy(() => import('@/features/auth/pages/LoginPage'));
const DashboardPage = lazy(() => import('@/features/dashboard/pages/DashboardPage'));
const TransactionsPage = lazy(() => import('@/features/transactions/pages/TransactionsPage'));
const ConsentsPage = lazy(() => import('@/features/consents/pages/ConsentsPage'));

export const AppRoutes: React.FC = () => {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        
        <Route path="/" element={<AppLayout />}>
          <Route index element={<DashboardPage />} />
          <Route path="transacoes" element={<TransactionsPage />} />
          <Route path="conexoes" element={<ConsentsPage />} />
        </Route>

        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  );
};
