import React, { useState } from 'react';
import { Card } from '@/shared/components/Card/Card';
import { CustomSelect } from '@/shared/components/Select/CustomSelect';
import { Modal } from '@/shared/components/Modal/Modal';
import { formatCurrencyBRL, formatDateBR, maskSensitiveAccount } from '@/shared/utils/formatters';
import { Landmark, ArrowUpRight, ArrowDownRight, Eye, Tag, Loader2 } from 'lucide-react';
import { useTransactionsQuery } from '../hooks/useTransactionsQuery';
import type { TransactionDto } from '../types/transactions.types';

export const TransactionsPage: React.FC = () => {
  const [selectedBank, setSelectedBank] = useState('all');
  const [selectedTransaction, setSelectedTransaction] = useState<TransactionDto | null>(null);

  const { data, isLoading, error } = useTransactionsQuery(1, selectedBank);
  const transactions = data?.items ?? [];

  const bankOptions = [
    { value: 'all', label: 'Todas as Instituições' },
    { value: 'itau', label: 'Itaú Unibanco', badge: 'Meu.Pluggy Open Finance' },
    { value: 'inter', label: 'Banco Inter', badge: 'Meu.Pluggy Open Finance' },
    { value: 'mercadopago', label: 'Mercado Pago', badge: 'Meu.Pluggy Open Finance' },
  ];

  const filteredTransactions = transactions.filter((t) => {
    if (selectedBank === 'itau') return t.bank?.toLowerCase().includes('itaú') || t.bank?.toLowerCase().includes('itau');
    if (selectedBank === 'inter') return t.bank?.toLowerCase().includes('inter');
    if (selectedBank === 'mercadopago') return t.bank?.toLowerCase().includes('mercado');
    return true;
  });

  return (
    <div className="flex flex-col gap-6 select-none">
      {/* Header e Filtros */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h1 className="section-title text-xl font-extrabold text-secondary">
            Extrato de Transações Ingestadas
          </h1>
          <p className="text-xs text-slate-500 font-medium mt-1">
            Ledger canônico deduplicado (SHA-256) via TransactionAggregator
          </p>
        </div>
        <div className="w-64">
          <CustomSelect
            options={bankOptions}
            value={selectedBank}
            onChange={setSelectedBank}
            label="Filtrar Instituição"
          />
        </div>
      </div>

      {isLoading && (
        <div className="flex items-center justify-center p-12 text-slate-400 gap-2">
          <Loader2 className="w-5 h-5 animate-spin text-brand" />
          <span className="text-xs font-semibold">Carregando extrato de transações...</span>
        </div>
      )}

      {error && (
        <Card className="p-4 border-status-danger/30 bg-status-danger-bg text-status-danger text-xs font-semibold">
          Não foi possível carregar as transações no momento.
        </Card>
      )}

      {!isLoading && (
        /* Table Card */
        <Card className="p-0 overflow-hidden" hoverable={false}>
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead>
                <tr className="bg-secondary text-white font-semibold uppercase tracking-wider text-[11px]">
                  <th className="px-6 py-4">Data</th>
                  <th className="px-6 py-4">Descrição</th>
                  <th className="px-6 py-4">Instituição e Conta</th>
                  <th className="px-6 py-4">Categoria</th>
                  <th className="px-6 py-4">Pagamento</th>
                  <th className="px-6 py-4 text-right">Valor</th>
                  <th className="px-6 py-4 text-center">Ações</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border-subtle bg-white">
                {filteredTransactions.length === 0 ? (
                  <tr>
                    <td colSpan={7} className="px-6 py-12 text-center text-slate-400 font-medium">
                      Nenhuma transação encontrada para a busca.
                    </td>
                  </tr>
                ) : (
                  filteredTransactions.map((t) => (
                    <tr key={t.id} className="hover:bg-secondary-light/40 transition-colors duration-150">
                      <td className="px-6 py-4 text-slate-500 font-semibold">{formatDateBR(t.date)}</td>
                      <td className="px-6 py-4 font-bold text-slate-800">{t.description}</td>
                      <td className="px-6 py-4 text-slate-600">
                        <div className="flex flex-col">
                          <span className="inline-flex items-center gap-1.5 font-bold text-slate-800">
                            <Landmark className="w-3.5 h-3.5 text-secondary" />
                            {t.bank}
                          </span>
                          <span className="text-[10px] text-slate-400 font-mono">
                            Conta {maskSensitiveAccount(t.accountNumber)}
                          </span>
                        </div>
                      </td>
                      <td className="px-6 py-4">
                        <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-[11px] font-bold bg-secondary-light text-secondary-dark">
                          <Tag className="w-3 h-3 text-secondary" />
                          {t.category}
                        </span>
                      </td>
                      <td className="px-6 py-4 text-slate-600 font-medium">
                        {t.paymentMethod} {t.installment && <span className="font-bold text-brand ml-1">({t.installment})</span>}
                      </td>
                      <td className="px-6 py-4 text-right font-extrabold text-sm">
                        <span className={t.type === 'INCOME' ? 'text-status-success inline-flex items-center gap-1' : 'text-status-danger inline-flex items-center gap-1'}>
                          {t.type === 'INCOME' ? <ArrowUpRight className="w-4 h-4" /> : <ArrowDownRight className="w-4 h-4" />}
                          {t.type === 'INCOME' ? '+ ' : '- '}
                          {formatCurrencyBRL(t.amount)}
                        </span>
                      </td>
                      <td className="px-6 py-4 text-center">
                        <button
                          type="button"
                          onClick={() => setSelectedTransaction(t)}
                          aria-label={`Ver detalhes da transação ${t.description}`}
                          title="Ver Detalhes"
                          className="p-1.5 text-slate-400 hover:text-brand hover:bg-brand-light rounded-lg transition-all duration-200 cursor-pointer"
                        >
                          <Eye className="w-4 h-4" />
                        </button>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </Card>
      )}

      {/* Modal de Detalhes da Transação */}
      <Modal
        isOpen={!!selectedTransaction}
        onClose={() => setSelectedTransaction(null)}
        title="Detalhes da Transação Ingestada"
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
                <span className={selectedTransaction.type === 'INCOME' ? 'text-sm font-extrabold text-status-success' : 'text-sm font-extrabold text-status-danger'}>
                  {selectedTransaction.type === 'INCOME' ? '+ ' : '- '}
                  {formatCurrencyBRL(selectedTransaction.amount)}
                </span>
              </div>

              <div className="p-3 rounded-xl bg-surface-ground border border-border-subtle flex flex-col gap-1">
                <span className="text-slate-400 font-semibold">Data da Transação</span>
                <span className="text-sm font-bold text-slate-800">{formatDateBR(selectedTransaction.date)}</span>
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="p-3 rounded-xl bg-surface-ground border border-border-subtle flex flex-col gap-1">
                <span className="text-slate-400 font-semibold">Instituição e Conta (LGPD)</span>
                <span className="text-sm font-bold text-slate-800">{selectedTransaction.bank} • {maskSensitiveAccount(selectedTransaction.accountNumber)}</span>
              </div>

              <div className="p-3 rounded-xl bg-surface-ground border border-border-subtle flex flex-col gap-1">
                <span className="text-slate-400 font-semibold">Meio de Pagamento</span>
                <span className="text-sm font-bold text-slate-800">
                  {selectedTransaction.paymentMethod} {selectedTransaction.installment && `(${selectedTransaction.installment})`}
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
