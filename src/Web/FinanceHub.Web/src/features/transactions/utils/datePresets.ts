import type { DatePresetKey } from '../types/transactions.types';

function formatYmd(year: number, monthZeroIndexed: number, day: number): string {
  const y = String(year).padStart(4, '0');
  const m = String(monthZeroIndexed + 1).padStart(2, '0');
  const d = String(day).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

export function getPresetDateRange(preset: DatePresetKey): { startDate?: string; endDate?: string } {
  const now = new Date();
  const year = now.getFullYear();
  const month = now.getMonth(); // 0-indexed

  switch (preset) {
    case 'current-month': {
      const daysInMonth = new Date(year, month + 1, 0).getDate();
      return {
        startDate: formatYmd(year, month, 1),
        endDate: formatYmd(year, month, daysInMonth),
      };
    }
    case 'previous-month': {
      const prevYear = month === 0 ? year - 1 : year;
      const prevMonth = month === 0 ? 11 : month - 1;
      const daysInPrevMonth = new Date(prevYear, prevMonth + 1, 0).getDate();
      return {
        startDate: formatYmd(prevYear, prevMonth, 1),
        endDate: formatYmd(prevYear, prevMonth, daysInPrevMonth),
      };
    }
    case 'last-30': {
      const past30 = new Date(now.getFullYear(), now.getMonth(), now.getDate() - 30);
      return {
        startDate: formatYmd(past30.getFullYear(), past30.getMonth(), past30.getDate()),
        endDate: formatYmd(now.getFullYear(), now.getMonth(), now.getDate()),
      };
    }
    case 'current-year': {
      return {
        startDate: formatYmd(year, 0, 1),
        endDate: formatYmd(year, 11, 31),
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
