import React, { useState } from 'react';
import { Modal } from '@/shared/components/Modal/Modal';
import { formatCurrencyBRL, formatDateBR, formatTimeBR, formatPaymentMethod, maskSensitiveAccount } from '@/shared/utils/formatters';
import { useTransactionsQuery } from '../hooks/useTransactionsQuery';
import { TransactionsSummaryCards } from '../components/TransactionsSummaryCards';
import { TransactionsFilterBar, getPresetDateRange } from '../components/TransactionsFilterBar';
import { TransactionsTable } from '../components/TransactionsTable';
import { TransactionsPagination } from '../components/TransactionsPagination';
import { PageContainer } from '@/shared/components/PageContainer/PageContainer';
import { useToggleNeutralityMutation } from '../hooks/useToggleNeutralityMutation';
import { ArrowLeftRight } from 'lucide-react';
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

  const [selectedTransaction, setSelectedTransaction] = useState<TransactionDto | null>(null);

  const { data, isLoading } = useTransactionsQuery(filters);
  const toggleNeutralityMutation = useToggleNeutralityMutation();

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
    setSelectedTransaction((prev) => prev ? { ...prev, isIgnoredInTotals: nextIgnored } : null);
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
        onSelectTransaction={setSelectedTransaction}
        onToggleNeutrality={handleToggleNeutrality}
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

      {/* Modal de Detalhes da Transação */}
      <Modal
        isOpen={!!selectedTransaction}
        onClose={() => setSelectedTransaction(null)}
        title="Detalhes da Transação"
      >
        {selectedTransaction && (
          <div className="flex flex-col gap-4 text-xs">
            {/* Card Principal de Destaque */}
            <div className="p-4 rounded-2xl bg-surface-ground border border-border-subtle flex flex-col gap-3">
              <div className="flex items-start justify-between gap-3">
                <div className="flex flex-col">
                  <span className="text-[11px] font-semibold text-slate-400 uppercase tracking-wider">
                    Lançamento
                  </span>
                  <span className="text-base font-bold text-secondary">
                    {selectedTransaction.description}
                  </span>
                </div>
                <span
                  className={`text-base font-black tabular-nums tracking-tight px-3 py-1 rounded-xl border ${
                    selectedTransaction.type === 'Credit'
                      ? 'bg-status-success-bg text-status-success border-status-success/20'
                      : 'bg-status-danger-bg text-status-danger border-status-danger/20'
                  }`}
                >
                  {selectedTransaction.type === 'Credit' ? '+ ' : '- '}
                  {formatCurrencyBRL(selectedTransaction.amount)}
                </span>
              </div>

              {selectedTransaction.merchantName && (
                <div className="flex items-center gap-2 pt-2 border-t border-border-subtle text-slate-500 font-medium">
                  <span className="text-[11px] text-slate-400">Estabelecimento:</span>
                  <span>{selectedTransaction.merchantName}</span>
                </div>
              )}
            </div>

            {/* Controle de Neutralidade / Dinheiro de Trânsito */}
            <div className="p-4 rounded-2xl bg-surface-card border border-border-subtle flex items-center justify-between gap-3">
              <div className="flex flex-col gap-0.5">
                <div className="flex items-center gap-1.5 font-bold text-slate-800">
                  <ArrowLeftRight className="w-4 h-4 text-brand" />
                  <span>Dinheiro de Trânsito / Lançamento Neutro</span>
                </div>
                <p className="text-[11px] text-slate-500">
                  Quando ativo, este lançamento é expurgado dos somatórios de receitas e despesas operacionais.
                </p>
              </div>

              <button
                type="button"
                onClick={() => handleToggleNeutrality(selectedTransaction)}
                disabled={toggleNeutralityMutation.isPending}
                aria-pressed={Boolean(selectedTransaction.isIgnoredInTotals)}
                className={`relative inline-flex h-6 w-11 shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none focus:ring-2 focus:ring-brand focus:ring-offset-2 ${
                  selectedTransaction.isIgnoredInTotals ? 'bg-brand' : 'bg-slate-300'
                }`}
              >
                <span className="sr-only">Alternar neutralidade nos totais</span>
                <span
                  className={`pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow-sm ring-0 transition duration-200 ease-in-out ${
                    selectedTransaction.isIgnoredInTotals ? 'translate-x-5' : 'translate-x-0'
                  }`}
                />
              </button>
            </div>

            {/* Grid de Metadados Bancários */}
            <div className="grid grid-cols-2 gap-3">
              <div className="p-3 rounded-xl bg-surface-ground border border-border-subtle flex flex-col gap-1">
                <span className="text-[10px] font-semibold text-slate-400 uppercase">Data e Hora</span>
                <span className="font-mono font-medium text-slate-700">
                  {formatDateBR(selectedTransaction.transactionDateUtc)} às {formatTimeBR(selectedTransaction.transactionDateUtc)}
                </span>
              </div>

              <div className="p-3 rounded-xl bg-surface-ground border border-border-subtle flex flex-col gap-1">
                <span className="text-[10px] font-semibold text-slate-400 uppercase">Meio de Pagamento</span>
                <span className="font-mono font-medium text-slate-700">
                  {formatPaymentMethod(selectedTransaction.channel)}
                </span>
              </div>

              <div className="p-3 rounded-xl bg-surface-ground border border-border-subtle flex flex-col gap-1">
                <span className="text-[10px] font-semibold text-slate-400 uppercase">Conta Vinculada</span>
                <span className="font-mono font-medium text-slate-700">
                  {maskSensitiveAccount(selectedTransaction.accountNumber)}
                </span>
              </div>

              <div className="p-3 rounded-xl bg-surface-ground border border-border-subtle flex flex-col gap-1">
                <span className="text-[10px] font-semibold text-slate-400 uppercase">Origem da Categoria</span>
                <span className="font-mono font-medium text-slate-700">
                  {selectedTransaction.categorizationSource} {selectedTransaction.isManuallyCategorized ? '(Manual)' : '(Auto)'}
                </span>
              </div>
            </div>
          </div>
        )}
      </Modal>
    </PageContainer>
  );
};

export default TransactionsPage;
