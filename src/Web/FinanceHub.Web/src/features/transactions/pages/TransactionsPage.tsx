import React, { useState } from 'react';
import { Modal } from '@/shared/components/Modal/Modal';
import { formatCurrencyBRL, formatDateBR, maskSensitiveAccount } from '@/shared/utils/formatters';
import { useTransactionsQuery } from '../hooks/useTransactionsQuery';
import { TransactionsSummaryCards } from '../components/TransactionsSummaryCards';
import { TransactionsFilterBar } from '../components/TransactionsFilterBar';
import { TransactionsTable } from '../components/TransactionsTable';
import { TransactionsPagination } from '../components/TransactionsPagination';
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
    <div className="flex flex-col gap-6 select-none">
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h1 className="section-title text-xl font-extrabold text-secondary">
            Extrato de Transações
          </h1>
          <p className="text-xs text-slate-500 font-medium mt-1">
            Controle de fluxo de caixa e categorização inteligente
          </p>
        </div>
      </div>

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
            <div className="p-4 rounded-xl bg-surface-ground border border-border-subtle flex flex-col gap-1">
              <span className="text-slate-400 font-semibold">Descrição do Lançamento</span>
              <span className="text-base font-bold text-secondary">{selectedTransaction.description}</span>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="p-3 rounded-xl bg-surface-ground border border-border-subtle flex flex-col gap-1">
                <span className="text-slate-400 font-semibold">Valor Consolidado</span>
                <span
                  className={
                    selectedTransaction.type === 'Credit'
                      ? 'text-sm font-extrabold text-status-success'
                      : 'text-sm font-extrabold text-status-danger'
                  }
                >
                  {selectedTransaction.type === 'Credit' ? '+ ' : '- '}
                  {formatCurrencyBRL(selectedTransaction.amount)}
                </span>
              </div>

              <div className="p-3 rounded-xl bg-surface-ground border border-border-subtle flex flex-col gap-1">
                <span className="text-slate-400 font-semibold">Data da Transação</span>
                <span className="text-sm font-bold text-slate-800">
                  {formatDateBR(selectedTransaction.transactionDateUtc)}
                </span>
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="p-3 rounded-xl bg-surface-ground border border-border-subtle flex flex-col gap-1">
                <span className="text-slate-400 font-semibold">Instituição e Conta</span>
                <span className="text-sm font-bold text-slate-800">
                  {selectedTransaction.institutionId.toUpperCase()} • {maskSensitiveAccount(selectedTransaction.accountNumber)}
                </span>
              </div>

              <div className="p-3 rounded-xl bg-surface-ground border border-border-subtle flex flex-col gap-1">
                <span className="text-slate-400 font-semibold">Canal de Pagamento</span>
                <span className="text-sm font-bold text-slate-800">
                  {selectedTransaction.channel}
                </span>
              </div>
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
};

export default TransactionsPage;
