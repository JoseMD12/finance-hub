import type { DatePresetKey } from '../types/transactions.types';

export function getPresetDateRange(preset: DatePresetKey): { startDate?: string; endDate?: string } {
  const now = new Date();
  const year = now.getFullYear();
  const month = now.getMonth(); // 0-indexed

  switch (preset) {
    case 'current-month': {
      const start = new Date(Date.UTC(year, month, 1, 0, 0, 0));
      const end = new Date(Date.UTC(year, month + 1, 0, 23, 59, 59, 999));
      return {
        startDate: start.toISOString(),
        endDate: end.toISOString(),
      };
    }
    case 'previous-month': {
      const start = new Date(Date.UTC(year, month - 1, 1, 0, 0, 0));
      const end = new Date(Date.UTC(year, month, 0, 23, 59, 59, 999));
      return {
        startDate: start.toISOString(),
        endDate: end.toISOString(),
      };
    }
    case 'last-30': {
      const start = new Date(now.getTime() - 30 * 24 * 60 * 60 * 1000);
      return {
        startDate: start.toISOString(),
        endDate: now.toISOString(),
      };
    }
    case 'current-year': {
      const start = new Date(Date.UTC(year, 0, 1, 0, 0, 0));
      const end = new Date(Date.UTC(year, 11, 31, 23, 59, 59, 999));
      return {
        startDate: start.toISOString(),
        endDate: end.toISOString(),
      };
    }
    case 'all-time':
    default:
      return {
        startDate: undefined,
        endDate: undefined,
      };
  }
}
