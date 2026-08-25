import React from 'react';
import { Search, RotateCcw, X, SlidersHorizontal, Calendar, Landmark, ArrowUpRight, ArrowDownRight, ArrowLeftRight } from 'lucide-react';
import { CustomSelect } from '@/shared/components/Select/CustomSelect';
import { CategoryFilterSelect } from './CategoryFilterSelect';
import { cn } from '@/shared/utils/cn';
import { useCategoriesQuery } from '../hooks/useCategoriesQuery';
import { getInstitutionLogoUrl } from '@/shared/constants/institutions';
import { Switch } from '@/shared/components/Switch/Switch';
import type { TransactionFilterParams } from '../types/transactions.types';

export interface TransactionsFilterBarProps {
  filters: TransactionFilterParams;
  onFilterChange: (newFilters: Partial<TransactionFilterParams>) => void;
  onResetFilters: () => void;
  includeIgnoredInTotals?: boolean;
  onIncludeIgnoredChange?: (include: boolean) => void;
}

export type DatePresetKey = 'current-month' | 'previous-month' | 'last-30' | 'current-year' | 'all-time';

interface DatePresetOption {
  key: DatePresetKey;
  label: string;
}

const DATE_PRESET_OPTIONS: DatePresetOption[] = [
  { key: 'current-month', label: 'Mês Atual' },
  { key: 'previous-month', label: 'Mês Anterior' },
  { key: 'last-30', label: 'Últimos 30 Dias' },
  { key: 'current-year', label: 'Ano Atual' },
  { key: 'all-time', label: 'Todo o Histórico' },
];

export const TransactionsFilterBar: React.FC<TransactionsFilterBarProps> = ({
  filters,
  onFilterChange,
  onResetFilters,
  includeIgnoredInTotals,
  onIncludeIgnoredChange,
}) => {
  const { data: categories = [] } = useCategoriesQuery();

  const institutionOptions = [
    {
      value: '',
      label: 'Todas as Instituições',
      icon: <Landmark className="w-3.5 h-3.5 text-brand shrink-0" />,
    },
    {
      value: 'itau',
      label: 'Itaú Unibanco',
      icon: <img src={getInstitutionLogoUrl('itau') || ''} alt="" className="w-3.5 h-3.5 object-contain rounded-xs" />,
    },
    {
      value: 'inter',
      label: 'Banco Inter',
      icon: <img src={getInstitutionLogoUrl('inter') || ''} alt="" className="w-3.5 h-3.5 object-contain rounded-xs" />,
    },
    {
      value: 'mercadopago',
      label: 'Mercado Pago',
      icon: <img src={getInstitutionLogoUrl('mercadopago') || ''} alt="" className="w-3.5 h-3.5 object-contain rounded-xs" />,
    },
  ];

  const typeOptions = [
    {
      value: '',
      label: 'Todos os Tipos',
      icon: <ArrowLeftRight className="w-3.5 h-3.5 text-brand shrink-0" />,
    },
    {
      value: 'Debit',
      label: 'Saídas / Despesas',
      icon: <ArrowDownRight className="w-3.5 h-3.5 text-status-danger shrink-0" />,
    },
    {
      value: 'Credit',
      label: 'Entradas / Receitas',
      icon: <ArrowUpRight className="w-3.5 h-3.5 text-status-success shrink-0" />,
    },
  ];

  const allCategories = React.useMemo(() => {
    const list: { id: string; name: string }[] = [];
    categories.forEach((cat) => {
      list.push({ id: cat.id, name: cat.name });
      if (cat.subcategories) {
        cat.subcategories.forEach((sub) => list.push({ id: sub.id, name: sub.name }));
      }
    });
    return list;
  }, [categories]);

  const activePreset = (filters.datePreset as DatePresetKey) || 'current-month';

  const activeFiltersCount =
    (filters.search ? 1 : 0) +
    (filters.institutionId ? 1 : 0) +
    (filters.categoryId ? 1 : 0) +
    (filters.type ? 1 : 0) +
    (activePreset !== 'current-month' ? 1 : 0);

  const selectedInstitutionLabel = institutionOptions.find(
    (o) => o.value === filters.institutionId
  )?.label;
  const selectedTypeLabel = typeOptions.find((o) => o.value === filters.type)?.label;
  const selectedCategoryLabel = allCategories.find(
    (o) => o.id === filters.categoryId
  )?.name;

  const handleSelectDatePreset = (preset: DatePresetKey) => {
    const range = getPresetDateRange(preset);
    onFilterChange({
      startDate: range.startDate,
      endDate: range.endDate,
      datePreset: preset,
      page: 1,
    });
  };

  return (
    <div className="p-4 bg-surface-card rounded-2xl border border-border-subtle shadow-card flex flex-col gap-4">
      {/* Grid Principal de Filtros */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3 items-end">
        {/* Busca textual */}
        <div className="flex flex-col gap-1.5 w-full">
          <label htmlFor="transactions-search-input" className="text-xs font-semibold text-slate-700 pl-1">
            Buscar por Termo
          </label>
          <div className="relative flex items-center h-10">
            <Search className="w-4 h-4 absolute left-3.5 text-slate-400 pointer-events-none" />
            <input
              id="transactions-search-input"
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
          <CategoryFilterSelect
            value={filters.categoryId ?? ''}
            onChange={(val) => onFilterChange({ categoryId: val, page: 1 })}
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

      {/* Barra de Presets Rápidos de Período e Switch de Totais Neutros */}
      <div className="flex flex-wrap items-center justify-between gap-3 pt-3 pb-0.5 border-t border-border-subtle/60">
        <div className="flex flex-wrap items-center gap-2">
          <span className="text-xs font-semibold text-slate-500 inline-flex items-center gap-1.5 mr-1 pl-1">
            <Calendar className="w-3.5 h-3.5 text-brand" />
            Período:
          </span>
          <div className="flex flex-wrap items-center gap-1.5">
            {DATE_PRESET_OPTIONS.map(({ key, label }) => {
              const isSelected = activePreset === key;

              return (
                <button
                  key={key}
                  type="button"
                  onClick={() => handleSelectDatePreset(key)}
                  aria-pressed={isSelected}
                  className={cn(
                    'px-3 py-1.5 rounded-xl text-xs font-semibold border transition-all duration-150 cursor-pointer select-none',
                    isSelected
                      ? 'bg-brand-light text-brand-dark border-brand font-bold shadow-2xs ring-1 ring-brand/20'
                      : 'bg-surface-ground text-slate-600 border-border-subtle hover:bg-slate-200/60 hover:text-slate-800'
                  )}
                >
                  {label}
                </button>
              );
            })}
          </div>
        </div>

        {/* Novo Switch para controle de cálculo de Neutros / Transferências */}
        <div className="p-2 rounded-xl bg-surface-ground border border-border-subtle flex items-center gap-2">
          <Switch
            checked={Boolean(includeIgnoredInTotals)}
            onChange={(val) => onIncludeIgnoredChange?.(val)}
            label="Incluir Transferências / Neutros nos Totais"
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
            <span className="text-slate-400 italic">Mês Atual (Padrão)</span>
          ) : (
            <>
              {activePreset && (
                <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-lg bg-surface-ground border border-border-subtle text-slate-700 font-medium">
                  {DATE_PRESET_OPTIONS.find(p => p.key === activePreset)?.label ?? activePreset}
                </span>
              )}

              {filters.search && (
                <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-lg bg-surface-ground border border-border-subtle text-slate-700 font-medium">
                  Busca: &ldquo;{filters.search}&rdquo;
                  <button
                    type="button"
                    onClick={() => onFilterChange({ search: undefined, page: 1 })}
                    className="hover:text-brand transition-colors cursor-pointer"
                    aria-label="Remover filtro de busca"
                  >
                    <X className="w-3.5 h-3.5" />
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
                    <X className="w-3.5 h-3.5" />
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
                    <X className="w-3.5 h-3.5" />
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
                    <X className="w-3.5 h-3.5" />
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
            Restaurar Padrão
          </button>
        )}
      </div>
    </div>
  );
};
