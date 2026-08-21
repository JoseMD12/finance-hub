import React, { useState } from 'react';
import { Modal } from '@/shared/components/Modal/Modal';
import { formatCurrencyBRL, formatDateBR, formatTimeBR, formatPaymentMethod, maskSensitiveAccount } from '@/shared/utils/formatters';
import { useTransactionsQuery } from '../hooks/useTransactionsQuery';
import { TransactionsSummaryCards } from '../components/TransactionsSummaryCards';
import { TransactionsFilterBar } from '../components/TransactionsFilterBar';
import { TransactionsTable } from '../components/TransactionsTable';
import { TransactionsPagination } from '../components/TransactionsPagination';
import { PageContainer } from '@/shared/components/PageContainer/PageContainer';
import type { TransactionDto, TransactionFilterParams } from '../types/transactions.types';

export const TransactionsPage: React.FC = () => {
  const [filters, setFilters] = useState<TransactionFilterParams>({
    page: 1,
    pageSize: 20,
  });

  const [selectedTransaction, setSelectedTransaction] = useState<TransactionDto | null>(null);

  const { data, isLoading } = useTransactionsQuery(filters);

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
    setFilters({ page: 1, pageSize: 20 });
  };

  return (
    <PageContainer
      title="Extrato de Transações"
      description="Controle de fluxo de caixa e categorização inteligente"
      actions={
        !isLoading && totalItems > 0 ? (
          <span className="px-3 py-1.5 rounded-xl bg-surface-card border border-border-subtle text-xs font-semibold text-slate-600 shadow-sm select-none">
            <strong className="text-secondary tabular-nums">{totalItems}</strong> lançamentos registrados
          </span>
        ) : undefined
      }
    >
      {/* Resumo do Período */}
      <TransactionsSummaryCards summary={summary} isLoading={isLoading} />

      {/* Barra de Filtros */}
      <TransactionsFilterBar
        filters={filters}
        onFilterChange={handleFilterChange}
        onResetFilters={handleResetFilters}
      />

      {/* Tabela de Transações */}
      <TransactionsTable
        transactions={transactions}
        isLoading={isLoading}
        onSelectTransaction={setSelectedTransaction}
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
                <div className="text-[11px] text-slate-500 font-medium">
                  Estabelecimento: <strong className="text-slate-700">{selectedTransaction.merchantName}</strong>
                </div>
              )}
            </div>

            {/* Grid de Metadados */}
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div className="p-3 rounded-xl bg-surface-ground border border-border-subtle flex flex-col gap-1">
                <span className="text-slate-400 font-semibold">Data e Hora</span>
                <div className="flex flex-col">
                  <span className="text-sm font-bold text-slate-800 tabular-nums">
                    {formatDateBR(selectedTransaction.transactionDateUtc)}
                  </span>
                  <span className="text-xs font-semibold text-slate-400 font-mono tabular-nums">
                    {formatTimeBR(selectedTransaction.transactionDateUtc)}
                  </span>
                </div>
              </div>

              <div className="p-3 rounded-xl bg-surface-ground border border-border-subtle flex flex-col gap-1">
                <span className="text-slate-400 font-semibold">Instituição e Conta</span>
                <span className="text-sm font-bold text-slate-800">
                  {selectedTransaction.institutionId.toUpperCase()} • Conta {maskSensitiveAccount(selectedTransaction.accountNumber)}
                </span>
              </div>

              <div className="p-3 rounded-xl bg-surface-ground border border-border-subtle flex flex-col gap-1">
                <span className="text-slate-400 font-semibold">Meio de Pagamento</span>
                <span className="text-sm font-bold text-slate-800 font-mono">
                  {formatPaymentMethod(selectedTransaction.channel)}
                </span>
              </div>

              <div className="p-3 rounded-xl bg-surface-ground border border-border-subtle flex flex-col gap-1">
                <span className="text-slate-400 font-semibold">Origem da Categorização</span>
                <span className="text-sm font-bold text-slate-800">
                  {selectedTransaction.isManuallyCategorized
                    ? 'Categorizado Manualmente'
                    : selectedTransaction.categorizationSource || 'Regra Automática'}
                </span>
              </div>
            </div>

            {/* ID Canônico do Ledger */}
            <div className="p-3 rounded-xl bg-surface-ground border border-border-subtle flex flex-col gap-1">
              <span className="text-slate-400 font-semibold">ID Canônico no Ledger</span>
              <span className="text-[11px] font-mono text-slate-600 break-all select-all">
                {selectedTransaction.id}
              </span>
            </div>
          </div>
        )}
      </Modal>
    </PageContainer>
  );
};

export default TransactionsPage;
