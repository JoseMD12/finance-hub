import React, { useState, useEffect } from 'react';
import { 
  X, 
  Landmark, 
  Calendar, 
  CreditCard, 
  Hash, 
  Link2, 
  Tag, 
  Ban, 
  Receipt, 
  MessageSquare,
  Sparkles,
  User,
  Building2
} from 'lucide-react';
import { formatCurrencyBRL, formatDateBR, formatTimeBR, formatPaymentMethod, maskSensitiveAccount } from '@/shared/utils/formatters';
import { getInstitutionInfo } from '@/shared/constants/institutions';
import { CategoryCatalogList } from './CategoryCatalogList';
import { useCategoriesQuery } from '../hooks/useCategoriesQuery';
import { useCategorizeTransactionMutation } from '../hooks/useCategorizeTransactionMutation';
import { useToggleNeutralityMutation } from '../hooks/useToggleNeutralityMutation';
import { useToggleBillPaymentMutation } from '../hooks/useToggleBillPaymentMutation';
import { useUpdateTransactionNotesMutation } from '../hooks/useUpdateTransactionNotesMutation';
import { cn } from '@/shared/utils/cn';
import type { TransactionDto } from '../types/transactions.types';

export interface TransactionDetailsDrawerProps {
  transaction: TransactionDto | null;
  isOpen: boolean;
  onClose: () => void;
  onSelectPairedTransaction?: (pairedId: string) => void;
}

export const TransactionDetailsDrawer: React.FC<TransactionDetailsDrawerProps> = ({
  transaction,
  isOpen,
  onClose,
  onSelectPairedTransaction,
}) => {
  const [notes, setNotes] = useState<string>('');
  const [isEditingCategory, setIsEditingCategory] = useState(false);
  const [expandedParentIds, setExpandedParentIds] = useState<Set<string>>(new Set());

  const { data: categories = [], isLoading: isLoadingCategories } = useCategoriesQuery();
  const categorizeMutation = useCategorizeTransactionMutation();
  const neutralityMutation = useToggleNeutralityMutation();
  const billPaymentMutation = useToggleBillPaymentMutation();
  const notesMutation = useUpdateTransactionNotesMutation();

  useEffect(() => {
    if (transaction) {
      setNotes(transaction.notes || '');
      setIsEditingCategory(false);
    }
  }, [transaction]);

  if (!isOpen || !transaction) return null;

  const isIncome = transaction.type === 'Credit';
  const instInfo = getInstitutionInfo(transaction.institutionId);

  const handleNotesBlur = () => {
    const trimmed = notes.trim();
    if (trimmed !== (transaction.notes || '')) {
      notesMutation.mutate({
        transactionId: transaction.id,
        notes: trimmed || null,
      });
    }
  };

  const handleSelectCategory = (categoryId: string) => {
    categorizeMutation.mutate({
      transactionId: transaction.id,
      categoryId,
      createCustomRule: false,
    });
    setIsEditingCategory(false);
  };

  const toggleExpand = (parentId: string, e: React.MouseEvent) => {
    e.stopPropagation();
    setExpandedParentIds((prev) => {
      const next = new Set(prev);
      if (next.has(parentId)) {
        next.delete(parentId);
      } else {
        next.add(parentId);
      }
      return next;
    });
  };

  const getSourceLabel = (source: string) => {
    switch (source) {
      case 'UserManual':
        return { label: 'Manual (Usuário)', icon: User, color: 'text-brand bg-brand-light border-brand/20' };
      case 'GlobalRule':
        return { label: 'Dataset Brasil', icon: Building2, color: 'text-secondary bg-secondary-light border-secondary/20' };
      default:
        return { label: 'Padrão do Sistema', icon: Sparkles, color: 'text-slate-600 bg-slate-100 border-slate-200' };
    }
  };

  const sourceInfo = getSourceLabel(transaction.categorizationSource);
  const SourceIcon = sourceInfo.icon;

  return (
    <div className="fixed inset-0 z-50 overflow-hidden animate-in fade-in duration-200">
      {/* Backdrop */}
      <div 
        className="absolute inset-0 bg-slate-900/40 backdrop-blur-xs transition-opacity"
        onClick={onClose}
        aria-hidden="true"
      />

      <div className="fixed inset-y-0 right-0 pl-10 max-w-full flex">
        <div className="w-screen max-w-md bg-surface-card border-l border-border-subtle shadow-card flex flex-col justify-between animate-in slide-in-from-right duration-200">
          
          {/* Header */}
          <div className="p-6 border-b border-border-subtle bg-surface-ground/50 flex items-start justify-between">
            <div className="flex flex-col gap-2.5 max-w-[80%]">
              <div className="flex items-center gap-2 flex-wrap">
                <span className={cn('inline-flex items-center gap-1 px-2 py-0.5 rounded-md text-[11px] font-bold border', instInfo.tagClass)}>
                  <Landmark className="w-3 h-3" />
                  <span>{instInfo.code}</span>
                </span>

                {transaction.isBillPayment && (
                  <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-md text-[11px] font-bold bg-amber-50 text-amber-700 border border-amber-200">
                    <Receipt className="w-3 h-3" />
                    <span>Fatura</span>
                  </span>
                )}

                {transaction.isIgnoredInTotals && (
                  <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-md text-[11px] font-bold bg-slate-100 text-slate-600 border border-slate-200">
                    <Ban className="w-3 h-3" />
                    <span>Neutro</span>
                  </span>
                )}
              </div>

              <h2 className="text-base font-bold text-slate-900 leading-tight break-words">
                {transaction.description}
              </h2>

              <div className="flex items-baseline gap-1.5">
                <span className={cn('text-2xl font-black tracking-tight', isIncome ? 'text-emerald-600' : 'text-slate-900')}>
                  {isIncome ? '+' : '-'}{formatCurrencyBRL(transaction.amount)}
                </span>
                <span className="text-xs font-semibold text-slate-400">BRL</span>
              </div>
            </div>

            <button
              type="button"
              onClick={onClose}
              className="p-2 rounded-lg text-slate-400 hover:text-slate-700 hover:bg-slate-100 active:scale-95 transition-all cursor-pointer"
              aria-label="Fechar detalhes"
            >
              <X className="w-5 h-5" />
            </button>
          </div>

          {/* Content Body */}
          <div className="flex-1 overflow-y-auto p-6 flex flex-col gap-6 divide-y divide-border-subtle/60">
            
            {/* Quick Actions */}
            <div className="flex flex-col gap-3">
              <h3 className="text-xs font-bold uppercase tracking-wider text-slate-400">Ações Rápidas</h3>
              <div className="grid grid-cols-2 gap-2">
                <button
                  type="button"
                  onClick={() => neutralityMutation.mutate({
                    transactionId: transaction.id,
                    isIgnoredInTotals: !transaction.isIgnoredInTotals
                  })}
                  className={cn(
                    'flex items-center justify-center gap-2 px-3 py-2 rounded-xl text-xs font-semibold border transition-all cursor-pointer',
                    transaction.isIgnoredInTotals
                      ? 'bg-slate-900 text-white border-slate-900 shadow-xs'
                      : 'bg-white text-slate-700 border-border-subtle hover:bg-slate-50'
                  )}
                >
                  <Ban className="w-3.5 h-3.5" />
                  <span>{transaction.isIgnoredInTotals ? 'Incluir em Totais' : 'Ignorar em Totais'}</span>
                </button>

                <button
                  type="button"
                  onClick={() => billPaymentMutation.mutate({
                    transactionId: transaction.id,
                    isBillPayment: !transaction.isBillPayment
                  })}
                  className={cn(
                    'flex items-center justify-center gap-2 px-3 py-2 rounded-xl text-xs font-semibold border transition-all cursor-pointer',
                    transaction.isBillPayment
                      ? 'bg-amber-600 text-white border-amber-600 shadow-xs'
                      : 'bg-white text-slate-700 border-border-subtle hover:bg-slate-50'
                  )}
                >
                  <Receipt className="w-3.5 h-3.5" />
                  <span>{transaction.isBillPayment ? 'Desmarcar Fatura' : 'Marcar Fatura'}</span>
                </button>
              </div>
            </div>

            {/* Categorization Details */}
            <div className="pt-5 flex flex-col gap-3">
              <div className="flex items-center justify-between">
                <h3 className="text-xs font-bold uppercase tracking-wider text-slate-400">Categoria</h3>
                <button
                  type="button"
                  onClick={() => setIsEditingCategory(!isEditingCategory)}
                  className="text-xs font-semibold text-brand hover:underline cursor-pointer"
                >
                  {isEditingCategory ? 'Concluir' : 'Alterar'}
                </button>
              </div>

              {isEditingCategory ? (
                <div className="p-3 bg-surface-ground border border-border-subtle rounded-xl max-h-52 overflow-y-auto">
                  <CategoryCatalogList
                    isLoading={isLoadingCategories}
                    isSearching={false}
                    categories={categories}
                    filteredSearchCategories={[]}
                    selectedCategoryId={transaction.categoryId}
                    expandedParentIds={expandedParentIds}
                    onSelectCategory={handleSelectCategory}
                    onToggleExpand={toggleExpand}
                  />
                </div>
              ) : (
                <div className="flex items-center justify-between p-3 rounded-xl bg-surface-ground border border-border-subtle">
                  <div className="flex items-center gap-2">
                    <Tag className="w-4 h-4 text-brand" />
                    <span className="text-xs font-bold text-slate-800">
                      {categories.find(c => c.id === transaction.categoryId)?.name || 'Categoria Desconhecida'}
                    </span>
                  </div>
                  <span className={cn('inline-flex items-center gap-1 px-2 py-0.5 rounded-md text-[10px] font-bold border', sourceInfo.color)}>
                    <SourceIcon className="w-3 h-3" />
                    <span>{sourceInfo.label}</span>
                  </span>
                </div>
              )}
            </div>

            {/* Open Finance Metadata */}
            <div className="pt-5 flex flex-col gap-3">
              <h3 className="text-xs font-bold uppercase tracking-wider text-slate-400">Metadados do Open Finance</h3>
              
              <div className="grid grid-cols-2 gap-3 text-xs">
                <div className="flex flex-col gap-1 p-3 rounded-xl bg-surface-ground border border-border-subtle">
                  <span className="text-slate-400 font-medium flex items-center gap-1.5">
                    <Calendar className="w-3.5 h-3.5 text-slate-500" />
                    Data e Hora
                  </span>
                  <span className="font-semibold text-slate-800">
                    {formatDateBR(transaction.transactionDateUtc)} às {formatTimeBR(transaction.transactionDateUtc)}
                  </span>
                </div>

                <div className="flex flex-col gap-1 p-3 rounded-xl bg-surface-ground border border-border-subtle">
                  <span className="text-slate-400 font-medium flex items-center gap-1.5">
                    <CreditCard className="w-3.5 h-3.5 text-slate-500" />
                    Meio de Pagamento
                  </span>
                  <span className="font-semibold text-slate-800">
                    {formatPaymentMethod(transaction.channel)}
                  </span>
                </div>

                <div className="flex flex-col gap-1 p-3 rounded-xl bg-surface-ground border border-border-subtle">
                  <span className="text-slate-400 font-medium flex items-center gap-1.5">
                    <Landmark className="w-3.5 h-3.5 text-slate-500" />
                    Conta Bancária
                  </span>
                  <span className="font-semibold text-slate-800">
                    {maskSensitiveAccount(transaction.accountNumber)}
                  </span>
                </div>

                <div className="flex flex-col gap-1 p-3 rounded-xl bg-surface-ground border border-border-subtle">
                  <span className="text-slate-400 font-medium flex items-center gap-1.5">
                    <Building2 className="w-3.5 h-3.5 text-slate-500" />
                    Estabelecimento
                  </span>
                  <span className="font-semibold text-slate-800 truncate" title={transaction.merchantName}>
                    {transaction.merchantName || 'Não identificado'}
                  </span>
                </div>
              </div>
            </div>

            {/* Deduplication & Pairing Details */}
            <div className="pt-5 flex flex-col gap-3">
              <h3 className="text-xs font-bold uppercase tracking-wider text-slate-400">Deduplicação & Pareamento</h3>
              
              {transaction.pairedTransactionId && (
                <div className="p-3 rounded-xl bg-brand-light/60 border border-brand/20 flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <Link2 className="w-4 h-4 text-brand" />
                    <div className="flex flex-col">
                      <span className="text-xs font-bold text-brand-dark">Transação Pareada Detectada</span>
                      <span className="text-[11px] text-slate-600">Transferência interna ou repasse vinculado</span>
                    </div>
                  </div>
                  {onSelectPairedTransaction && (
                    <button
                      type="button"
                      onClick={() => onSelectPairedTransaction(transaction.pairedTransactionId!)}
                      className="px-2.5 py-1 rounded-lg bg-brand text-white text-xs font-semibold shadow-2xs hover:bg-brand-dark cursor-pointer transition-all"
                    >
                      Ver Pareada
                    </button>
                  )}
                </div>
              )}

              <div className="p-3 rounded-xl bg-surface-ground border border-border-subtle flex flex-col gap-1">
                <span className="text-[11px] font-medium text-slate-400 flex items-center gap-1">
                  <Hash className="w-3 h-3" />
                  Hash SHA-256 de Idempotência
                </span>
                <span className="text-[10px] font-mono text-slate-600 break-all leading-tight">
                  {transaction.id}
                </span>
              </div>
            </div>

            {/* User Notes Area */}
            <div className="pt-5 flex flex-col gap-3">
              <div className="flex items-center justify-between">
                <h3 className="text-xs font-bold uppercase tracking-wider text-slate-400 flex items-center gap-1.5">
                  <MessageSquare className="w-3.5 h-3.5 text-slate-500" />
                  Observações do Usuário
                </h3>
                <span className="text-[10px] font-medium text-slate-400">
                  {notes.length}/500
                </span>
              </div>

              <textarea
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
                onBlur={handleNotesBlur}
                maxLength={500}
                placeholder="Escreva anotações ou detalhes sobre este lançamento..."
                className="w-full h-24 p-3 rounded-xl border border-border-subtle bg-surface-ground text-xs text-slate-800 placeholder-slate-400 focus:outline-none focus:border-brand focus:ring-2 focus:ring-brand/20 transition-all resize-none"
              />
            </div>
          </div>

          {/* Drawer Footer */}
          <div className="p-4 border-t border-border-subtle bg-surface-ground/50 flex justify-end">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 rounded-xl bg-slate-200 text-slate-700 text-xs font-bold hover:bg-slate-300 transition-all cursor-pointer"
            >
              Fechar
            </button>
          </div>

        </div>
      </div>
    </div>
  );
};
