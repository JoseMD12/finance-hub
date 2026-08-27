/**
 * Tipos de filtro compartilhados entre features.
 *
 * Vivem em `shared/` porque Dashboard e Transações usam o mesmo seletor de período, e a
 * Regra 2.2 proíbe que uma feature importe as entranhas de outra. São re-exportados de
 * `features/transactions/types/transactions.types.ts` para não quebrar imports existentes.
 */

export type DatePresetKey =
  | 'current-month'
  | 'previous-month'
  | 'last-30'
  | 'current-year'
  | 'all-time'
  | 'custom';

export type ChannelGroupFilter = 'account' | 'credit';

/** Intervalo resolvido a partir de um preset, no formato ISO `YYYY-MM-DD`. */
export interface DateRangeFilter {
  readonly startDate?: string;
  readonly endDate?: string;
}
