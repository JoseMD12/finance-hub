import React from 'react';
import { Search, RotateCcw } from 'lucide-react';
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

  return (
    <div className="p-4 bg-surface-card rounded-2xl border border-border-subtle shadow-card flex flex-col gap-4">
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
        {/* Busca textual */}
        <div className="relative flex items-center">
          <Search className="w-4 h-4 absolute left-3 text-slate-400" />
          <input
            type="text"
            placeholder="Buscar por descrição ou loja..."
            value={filters.search ?? ''}
            onChange={(e) => onFilterChange({ search: e.target.value, page: 1 })}
            className="w-full pl-9 pr-3 py-2 text-xs rounded-xl border border-border-subtle bg-surface-ground text-slate-800 focus:outline-none focus:border-brand transition-colors"
          />
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

      <div className="flex items-center justify-between pt-2 border-t border-border-subtle text-xs text-slate-500">
        <span>Filtros refinados para o extrato financeiro</span>
        <button
          type="button"
          onClick={onResetFilters}
          className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-slate-600 hover:text-brand hover:bg-brand-light transition-colors font-semibold cursor-pointer"
        >
          <RotateCcw className="w-3.5 h-3.5" />
          Limpar Filtros
        </button>
      </div>
    </div>
  );
};
