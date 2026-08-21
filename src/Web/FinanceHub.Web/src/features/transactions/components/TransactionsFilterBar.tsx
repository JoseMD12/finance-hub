import React from 'react';
import { Search, RotateCcw, X, SlidersHorizontal } from 'lucide-react';
import { CustomSelect } from '@/shared/components/Select/CustomSelect';
import { useCategoriesQuery } from '../hooks/useCategoriesQuery';
import type { TransactionFilterParams } from '../types/transactions.types';

export interface TransactionsFilterBarProps {
  filters: TransactionFilterParams;
  onFilterChange: (newFilters: Partial<TransactionFilterParams>) => void;
  onResetFilters: () => void;
}

export const TransactionsFilterBar: React.FC<TransactionsFilterBarProps> = ({
  filters,
  onFilterChange,
  onResetFilters,
}) => {
  const { data: categories = [] } = useCategoriesQuery();

  const institutionOptions = [
    { value: '', label: 'Todas as Instituições' },
    { value: 'itau', label: 'Itaú Unibanco' },
    { value: 'inter', label: 'Banco Inter' },
    { value: 'mercadopago', label: 'Mercado Pago' },
  ];

  const typeOptions = [
    { value: '', label: 'Todos os Tipos' },
    { value: 'Debit', label: 'Saídas / Despesas' },
    { value: 'Credit', label: 'Entradas / Receitas' },
  ];

  const categoryOptions = React.useMemo(() => {
    const opts = [{ value: '', label: 'Todas as Categorias' }];
    categories.forEach((cat) => {
      opts.push({ value: cat.id, label: cat.name });
      if (cat.subcategories) {
        cat.subcategories.forEach((sub) => {
          opts.push({ value: sub.id, label: `  • ${sub.name}` });
        });
      }
    });
    return opts;
  }, [categories]);

  const activeFiltersCount =
    (filters.search ? 1 : 0) +
    (filters.institutionId ? 1 : 0) +
    (filters.categoryId ? 1 : 0) +
    (filters.type ? 1 : 0);

  const selectedInstitutionLabel = institutionOptions.find(
    (o) => o.value === filters.institutionId
  )?.label;
  const selectedTypeLabel = typeOptions.find((o) => o.value === filters.type)?.label;
  const selectedCategoryLabel = categoryOptions.find(
    (o) => o.value === filters.categoryId
  )?.label?.trim();

  return (
    <div className="p-4 bg-surface-card rounded-2xl border border-border-subtle shadow-card flex flex-col gap-4">
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3 items-end">
        {/* Busca textual */}
        <div className="flex flex-col gap-1.5 w-full">
          <label className="text-xs font-semibold text-slate-700">Buscar por Termo</label>
          <div className="relative flex items-center h-10">
            <Search className="w-4 h-4 absolute left-3.5 text-slate-400 pointer-events-none" />
            <input
              type="text"
              placeholder="Descrição, loja ou estabelecimento..."
              value={filters.search ?? ''}
              onChange={(e) => onFilterChange({ search: e.target.value || undefined, page: 1 })}
              className="w-full h-full pl-10 pr-8 text-xs rounded-xl border border-border-subtle bg-surface-ground text-slate-800 focus:outline-none focus:border-brand focus:ring-2 focus:ring-brand/20 transition-all"
            />
            {filters.search && (
              <button
                type="button"
                onClick={() => onFilterChange({ search: undefined, page: 1 })}
                aria-label="Limpar busca"
                className="absolute right-2.5 p-1 rounded-md text-slate-400 hover:text-slate-600 hover:bg-slate-200/60 transition-colors cursor-pointer"
              >
                <X className="w-3.5 h-3.5" />
              </button>
            )}
          </div>
        </div>

        {/* Filtro Instituição */}
        <div>
          <CustomSelect
            options={institutionOptions}
            value={filters.institutionId ?? ''}
            onChange={(val) => onFilterChange({ institutionId: val || undefined, page: 1 })}
            label="Instituição"
          />
        </div>

        {/* Filtro Categoria */}
        <div>
          <CustomSelect
            options={categoryOptions}
            value={filters.categoryId ?? ''}
            onChange={(val) => onFilterChange({ categoryId: val || undefined, page: 1 })}
            label="Categoria"
          />
        </div>

        {/* Filtro Tipo */}
        <div>
          <CustomSelect
            options={typeOptions}
            value={filters.type ?? ''}
            onChange={(val) => onFilterChange({ type: val || undefined, page: 1 })}
            label="Tipo de Lançamento"
          />
        </div>
      </div>

      {/* Rodapé com filtros ativos e botão limpar */}
      <div className="flex flex-wrap items-center justify-between gap-2 pt-3 border-t border-border-subtle text-xs text-slate-500">
        <div className="flex flex-wrap items-center gap-1.5">
          <span className="inline-flex items-center gap-1 font-semibold text-slate-600 mr-1">
            <SlidersHorizontal className="w-3.5 h-3.5 text-brand" />
            Filtros ativos:
          </span>

          {activeFiltersCount === 0 ? (
            <span className="text-slate-400 italic">Nenhum filtro aplicado</span>
          ) : (
            <>
              {filters.search && (
                <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-lg bg-surface-ground border border-border-subtle text-slate-700 font-medium">
                  Busca: &ldquo;{filters.search}&rdquo;
                  <button
                    type="button"
                    onClick={() => onFilterChange({ search: undefined, page: 1 })}
                    className="hover:text-brand transition-colors cursor-pointer"
                    aria-label="Remover filtro de busca"
                  >
                    <X className="w-3 h-3" />
                  </button>
                </span>
              )}

              {filters.institutionId && selectedInstitutionLabel && (
                <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-lg bg-surface-ground border border-border-subtle text-slate-700 font-medium">
                  {selectedInstitutionLabel}
                  <button
                    type="button"
                    onClick={() => onFilterChange({ institutionId: undefined, page: 1 })}
                    className="hover:text-brand transition-colors cursor-pointer"
                    aria-label="Remover filtro de instituição"
                  >
                    <X className="w-3 h-3" />
                  </button>
                </span>
              )}

              {filters.categoryId && selectedCategoryLabel && (
                <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-lg bg-surface-ground border border-border-subtle text-slate-700 font-medium">
                  {selectedCategoryLabel}
                  <button
                    type="button"
                    onClick={() => onFilterChange({ categoryId: undefined, page: 1 })}
                    className="hover:text-brand transition-colors cursor-pointer"
                    aria-label="Remover filtro de categoria"
                  >
                    <X className="w-3 h-3" />
                  </button>
                </span>
              )}

              {filters.type && selectedTypeLabel && (
                <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-lg bg-surface-ground border border-border-subtle text-slate-700 font-medium">
                  {selectedTypeLabel}
                  <button
                    type="button"
                    onClick={() => onFilterChange({ type: undefined, page: 1 })}
                    className="hover:text-brand transition-colors cursor-pointer"
                    aria-label="Remover filtro de tipo"
                  >
                    <X className="w-3 h-3" />
                  </button>
                </span>
              )}
            </>
          )}
        </div>

        {activeFiltersCount > 0 && (
          <button
            type="button"
            onClick={onResetFilters}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-slate-600 hover:text-brand hover:bg-brand-light transition-colors font-semibold cursor-pointer"
          >
            <RotateCcw className="w-3.5 h-3.5" />
            Limpar Filtros
          </button>
        )}
      </div>
    </div>
  );
};
