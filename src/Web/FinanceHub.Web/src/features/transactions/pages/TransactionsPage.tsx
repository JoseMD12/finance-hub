import React, { useState } from 'react';
import { useTransactionsQuery } from '../hooks/useTransactionsQuery';
import { TransactionsSummaryCards } from '../components/TransactionsSummaryCards';
import { TransactionsFilterBar } from '../components/TransactionsFilterBar';
import { getPresetDateRange } from '../utils/datePresets';
import { TransactionsTable } from '../components/TransactionsTable';
import { TransactionsPagination } from '../components/TransactionsPagination';
import { PageContainer } from '@/shared/components/PageContainer/PageContainer';
import { useToggleNeutralityMutation } from '../hooks/useToggleNeutralityMutation';
import { useToggleBillPaymentMutation } from '../hooks/useToggleBillPaymentMutation';
import type { TransactionDto, TransactionFilterParams } from '../types/transactions.types';

export const TransactionsPage: React.FC = () => {
  const initialRange = getPresetDateRange('current-month');

  const [filters, setFilters] = useState<TransactionFilterParams>({
    page: 1,
    pageSize: 20,
    startDate: initialRange.startDate,
    endDate: initialRange.endDate,
    datePreset: 'current-month',
  });

  const { data, isLoading } = useTransactionsQuery(filters);
  const { mutateAsync: toggleNeutralityAsync } = useToggleNeutralityMutation();
  const { mutateAsync: toggleBillPaymentAsync } = useToggleBillPaymentMutation();

  const transactions = data?.items ?? [];
  const summary = data?.summary;

  const totalPages = data?.totalPages ?? 1;
  const totalItems = data?.totalItems ?? 0;
  const currentPage = filters.page ?? 1;
  const pageSize = filters.pageSize ?? 20;

  const handleFilterChange = React.useCallback((newFilters: Partial<TransactionFilterParams>) => {
    setFilters((prev) => ({ ...prev, ...newFilters }));
  }, []);

  const handleResetFilters = React.useCallback(() => {
    const range = getPresetDateRange('current-month');
    setFilters({
      page: 1,
      pageSize: 20,
      startDate: range.startDate,
      endDate: range.endDate,
      datePreset: 'current-month',
      includeIgnoredInTotals: false,
    });
  }, []);

  const handleToggleNeutrality = React.useCallback(async (transaction: TransactionDto) => {
    const nextIgnored = !transaction.isIgnoredInTotals;
    await toggleNeutralityAsync({
      transactionId: transaction.id,
      isIgnoredInTotals: nextIgnored,
      reason: nextIgnored ? 'Marcado manualmente como neutro/trânsito' : 'Reativado manualmente',
    });
  }, [toggleNeutralityAsync]);

  const handleToggleBillPayment = React.useCallback(async (transaction: TransactionDto) => {
    const nextIsBillPayment = !transaction.isBillPayment;
    await toggleBillPaymentAsync({
      transactionId: transaction.id,
      isBillPayment: nextIsBillPayment,
    });
  }, [toggleBillPaymentAsync]);

  const handleIncludeIgnoredChange = React.useCallback((include: boolean) => {
    handleFilterChange({ includeIgnoredInTotals: include });
  }, [handleFilterChange]);

  const handlePageChange = React.useCallback((page: number) => {
    setFilters((prev) => ({ ...prev, page }));
    window.scrollTo({ top: 0, behavior: 'auto' });
  }, []);

  const handlePageSizeChange = React.useCallback((newPageSize: number) => {
    setFilters((prev) => ({ ...prev, pageSize: newPageSize, page: 1 }));
    window.scrollTo({ top: 0, behavior: 'auto' });
  }, []);

  return (
    <PageContainer
      title="Extrato de Transações"
      description="Controle de fluxo de caixa e categorização inteligente"
    >
      {/* Resumo do Período */}
      <TransactionsSummaryCards summary={summary} isLoading={isLoading} />

      {/* Barra de Filtros */}
      <TransactionsFilterBar
        filters={filters}
        onFilterChange={handleFilterChange}
        onResetFilters={handleResetFilters}
        includeIgnoredInTotals={Boolean(filters.includeIgnoredInTotals)}
        onIncludeIgnoredChange={handleIncludeIgnoredChange}
      />

      {/* Tabela de Transações */}
      <TransactionsTable
        transactions={transactions}
        isLoading={isLoading}
        onToggleNeutrality={handleToggleNeutrality}
        onToggleBillPayment={handleToggleBillPayment}
      />

      {/* Paginação Clássica */}
      <TransactionsPagination
        currentPage={currentPage}
        totalPages={totalPages}
        pageSize={pageSize}
        totalItems={totalItems}
        onPageChange={handlePageChange}
        onPageSizeChange={handlePageSizeChange}
      />
    </PageContainer>
  );
};

export default TransactionsPage;
