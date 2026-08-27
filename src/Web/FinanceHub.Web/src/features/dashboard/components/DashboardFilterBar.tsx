import React from 'react';
import { Calendar, Landmark, RotateCcw } from 'lucide-react';
import { CustomSelect } from '@/shared/components/Select/CustomSelect';
import { DateRangePicker } from '@/shared/components/DateRangePicker/DateRangePicker';
import type { DateRangeValue } from '@/shared/components/DateRangePicker/DateRangePicker';
import { getInstitutionLogoUrl } from '@/shared/constants/institutions';
import { getPresetDateRange } from '@/shared/utils/datePresets';
import type { DatePresetKey } from '@/shared/types/filters.types';
import { cn } from '@/shared/utils/cn';
import type { DashboardFilterParams } from '../types/dashboard.types';

export interface DashboardFilterBarProps {
  filters: DashboardFilterParams;
  onFilterChange: (next: Partial<DashboardFilterParams>) => void;
  onResetFilters: () => void;
}

interface DatePresetOption {
  readonly key: DatePresetKey;
  readonly label: string;
}

// Declarado fora do componente para não recriar a instância a cada render (Regra 8.2).
const DATE_PRESET_OPTIONS: readonly DatePresetOption[] = [
  { key: 'current-month', label: 'Mês Atual' },
  { key: 'previous-month', label: 'Mês Anterior' },
  { key: 'last-30', label: 'Últimos 30 Dias' },
  { key: 'current-year', label: 'Ano Atual' },
  { key: 'all-time', label: 'Todo o Histórico' },
];

const DashboardFilterBarComponent: React.FC<DashboardFilterBarProps> = ({
  filters,
  onFilterChange,
  onResetFilters,
}) => {
  const institutionOptions = React.useMemo(
    () => [
      {
        value: '',
        label: 'Todas as Instituições',
        icon: <Landmark className="w-3.5 h-3.5 text-brand shrink-0" aria-hidden="true" />,
      },
      { value: 'itau', label: 'Itaú Unibanco' },
      { value: 'inter', label: 'Banco Inter' },
      { value: 'mercadopago', label: 'Mercado Pago' },
      { value: 'nubank', label: 'Nubank' },
    ].map((option) =>
      option.icon
        ? option
        : {
            ...option,
            icon: (
              <img
                src={getInstitutionLogoUrl(option.value) || ''}
                alt=""
                className="w-3.5 h-3.5 object-contain rounded-xs"
              />
            ),
          },
    ),
    [],
  );

  const handlePresetClick = React.useCallback(
    (preset: DatePresetKey) => {
      const range = getPresetDateRange(preset);
      onFilterChange({ datePreset: preset, startDate: range.startDate, endDate: range.endDate });
    },
    [onFilterChange],
  );

  const handleCustomRange = React.useCallback(
    (range: DateRangeValue) => {
      onFilterChange({
        datePreset: 'custom',
        startDate: range.startDate,
        endDate: range.endDate,
      });
    },
    [onFilterChange],
  );

  const handleInstitutionChange = React.useCallback(
    (value: string) => onFilterChange({ institutionId: value || undefined }),
    [onFilterChange],
  );

  return (
    <div className="flex flex-wrap items-center gap-3 p-4 bg-surface-card border border-border-subtle rounded-2xl shadow-card">
      <div className="flex items-center gap-1.5 text-slate-500 shrink-0">
        <Calendar className="w-4 h-4" aria-hidden="true" />
        <span className="text-xs font-semibold">Período</span>
      </div>

      <div className="flex flex-wrap items-center gap-1.5" role="group" aria-label="Presets de período">
        {DATE_PRESET_OPTIONS.map((option) => (
          <button
            key={option.key}
            type="button"
            onClick={() => handlePresetClick(option.key)}
            aria-pressed={filters.datePreset === option.key}
            className={cn(
              'px-3 py-1.5 text-xs font-semibold rounded-xl border transition-colors duration-200 cursor-pointer',
              filters.datePreset === option.key
                ? 'bg-brand text-white border-brand'
                : 'bg-surface-ground text-slate-600 border-border-subtle hover:border-slate-300',
            )}
          >
            {option.label}
          </button>
        ))}
      </div>

      <DateRangePicker
        startDate={filters.startDate}
        endDate={filters.endDate}
        onApply={handleCustomRange}
        isActive={filters.datePreset === 'custom'}
      />

      <div className="w-52">
        <CustomSelect
          value={filters.institutionId ?? ''}
          onChange={handleInstitutionChange}
          options={institutionOptions}
          label="Filtrar por instituição"
          size="sm"
        />
      </div>

      <button
        type="button"
        onClick={onResetFilters}
        aria-label="Limpar filtros do painel"
        className="ml-auto inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold text-slate-500 bg-surface-ground border border-border-subtle rounded-xl hover:border-slate-300 hover:text-secondary transition-colors duration-200 cursor-pointer"
      >
        <RotateCcw className="w-3.5 h-3.5" aria-hidden="true" />
        Limpar
      </button>
    </div>
  );
};

export const DashboardFilterBar = React.memo(DashboardFilterBarComponent);
