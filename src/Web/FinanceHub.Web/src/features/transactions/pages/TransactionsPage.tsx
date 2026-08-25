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
  const toggleNeutralityMutation = useToggleNeutralityMutation();
  const toggleBillPaymentMutation = useToggleBillPaymentMutation();

  const transactions = data?.items ?? [];
  const summary = data?.summary;

  const totalPages = data?.totalPages ?? 1;
  const totalItems = data?.totalItems ?? 0;
  const currentPage = filters.page ?? 1;
  const pageSize = filters.pageSize ?? 20;

  const handleFilterChange = (newFilters: Partial<TransactionFilterParams>) => {
    setFilters((prev) => ({ ...prev, ...newFilters }));
  };

  const handleResetFilters = () => {
    const range = getPresetDateRange('current-month');
    setFilters({
      page: 1,
      pageSize: 20,
      startDate: range.startDate,
      endDate: range.endDate,
      datePreset: 'current-month',
      includeIgnoredInTotals: false,
    });
  };

  const handleToggleNeutrality = async (transaction: TransactionDto) => {
    const nextIgnored = !transaction.isIgnoredInTotals;
    await toggleNeutralityMutation.mutateAsync({
      transactionId: transaction.id,
      isIgnoredInTotals: nextIgnored,
      reason: nextIgnored ? 'Marcado manualmente como neutro/trânsito' : 'Reativado manualmente',
    });
  };

  const handleToggleBillPayment = async (transaction: TransactionDto) => {
    const nextIsBillPayment = !transaction.isBillPayment;
    await toggleBillPaymentMutation.mutateAsync({
      transactionId: transaction.id,
      isBillPayment: nextIsBillPayment,
    });
  };

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
        onIncludeIgnoredChange={(include) => handleFilterChange({ includeIgnoredInTotals: include })}
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
        onPageChange={(page) => handleFilterChange({ page })}
        onPageSizeChange={(newPageSize) => handleFilterChange({ pageSize: newPageSize, page: 1 })}
      />
    </PageContainer>
  );
};

export default TransactionsPage;
