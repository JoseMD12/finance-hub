import React, { useState, useEffect, useCallback, useMemo } from 'react';
import ReactDOM from 'react-dom';
import { Calendar as CalendarIcon, ChevronLeft, ChevronRight, X, Check, RotateCcw } from 'lucide-react';
import { cn } from '@/shared/utils/cn';
import { useFloatingPopover } from '@/shared/hooks/useFloatingPopover';

export interface DateRangeValue {
  startDate?: string;
  endDate?: string;
}

export interface DateRangePickerProps {
  startDate?: string;
  endDate?: string;
  isActive?: boolean;
  onApply: (range: DateRangeValue) => void;
  className?: string;
}

interface CalendarDayCellProps {
  dayDate: Date;
  isCurrentMonth: boolean;
  tempStart?: string;
  tempEnd?: string;
  hoveredDate: Date | null;
  todayTimestamp: number;
  onDayClick: (d: Date) => void;
  onDayHover: (d: Date | null) => void;
}

/** Converte Date para string YYYY-MM-DD segura e imune a timezones */
function formatYmd(d: Date): string {
  const y = String(d.getFullYear()).padStart(4, '0');
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

/** Formata data YYYY-MM-DD ou ISO para visualização brasileira (DD/MM/AAAA) */
function formatDateDisplay(dateStr?: string): string {
  if (!dateStr) return '';
  if (dateStr.length >= 10 && dateStr.includes('-')) {
    const parts = dateStr.slice(0, 10).split('-');
    if (parts.length === 3) {
      const [year, month, day] = parts;
      return `${day}/${month}/${year}`;
    }
  }
  const date = new Date(dateStr);
  if (Number.isNaN(date.getTime())) return '';
  const day = String(date.getDate()).padStart(2, '0');
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const year = date.getFullYear();
  return `${day}/${month}/${year}`;
}

/** Aplica máscara automática de data DD/MM/AAAA para digitação */
function formatDisplayDateInput(val: string): string {
  const digits = val.replace(/\D/g, '').slice(0, 8);
  if (digits.length <= 2) return digits;
  if (digits.length <= 4) return `${digits.slice(0, 2)}/${digits.slice(2)}`;
  return `${digits.slice(0, 2)}/${digits.slice(2, 4)}/${digits.slice(4)}`;
}

/** Converte DD/MM/AAAA digitado para YYYY-MM-DD válido */
function parseDisplayDateToYmd(displayVal: string): string | null {
  const parts = displayVal.split('/');
  if (parts.length !== 3) return null;
  const [dayStr, monthStr, yearStr] = parts;
  if (dayStr.length !== 2 || monthStr.length !== 2 || yearStr.length !== 4) return null;
  const d = Number.parseInt(dayStr, 10);
  const m = Number.parseInt(monthStr, 10);
  const y = Number.parseInt(yearStr, 10);
  if (Number.isNaN(d) || Number.isNaN(m) || Number.isNaN(y)) return null;
  if (m < 1 || m > 12 || d < 1 || d > 31 || y < 1900 || y > 2100) return null;

  const daysInM = new Date(y, m, 0).getDate();
  if (d > daysInM) return null;

  return `${String(y).padStart(4, '0')}-${String(m).padStart(2, '0')}-${String(d).padStart(2, '0')}`;
}

/** Retorna o timestamp da meia-noite local de uma data */
function getMidnightTimestamp(d: Date): number {
  return new Date(d.getFullYear(), d.getMonth(), d.getDate(), 0, 0, 0, 0).getTime();
}

/** Converte string YYYY-MM-DD em objeto Date local */
function parseYmdToDate(dateStr?: string): Date | null {
  if (!dateStr) return null;
  const cleanStr = dateStr.slice(0, 10);
  const parts = cleanStr.split('-');
  if (parts.length !== 3) return null;
  const [y, m, d] = parts.map((p) => Number.parseInt(p, 10));
  if (Number.isNaN(y) || Number.isNaN(m) || Number.isNaN(d)) return null;
  return new Date(y, m - 1, d, 0, 0, 0, 0);
}

/** Calcula se a data está no intervalo selecionado ou hovered */
function getDayRangeState(
  dayTime: number,
  startTime: number | null,
  endTime: number | null,
  hoverTime: number | null
) {
  const isStart = startTime !== null && dayTime === startTime;
  const isEnd = endTime !== null && dayTime === endTime;
  const isInRange = startTime !== null && endTime !== null && dayTime > startTime && dayTime < endTime;

  const isHoverRange =
    startTime !== null &&
    endTime === null &&
    hoverTime !== null &&
    ((hoverTime >= startTime && dayTime > startTime && dayTime <= hoverTime) ||
      (hoverTime < startTime && dayTime < startTime && dayTime >= hoverTime));

  return { isStart, isEnd, isInRange, isHoverRange };
}

/** Helper para atualizar seleção de data por clique */
function computeNextDateRange(
  dayDate: Date,
  tempStart?: string,
  tempEnd?: string
): { nextStart: string; nextEnd?: string; nextStartDisplay: string; nextEndDisplay: string } {
  const clickedYmd = formatYmd(dayDate);
  const clickedDisplay = formatDateDisplay(clickedYmd);

  if (!tempStart || (tempStart && tempEnd)) {
    return {
      nextStart: clickedYmd,
      nextEnd: undefined,
      nextStartDisplay: clickedDisplay,
      nextEndDisplay: '',
    };
  }

  const startDateObj = parseYmdToDate(tempStart);
  if (!startDateObj) {
    return {
      nextStart: clickedYmd,
      nextEnd: undefined,
      nextStartDisplay: clickedDisplay,
      nextEndDisplay: '',
    };
  }

  const startMidnight = getMidnightTimestamp(startDateObj);
  const clickedMidnight = getMidnightTimestamp(dayDate);

  if (clickedMidnight < startMidnight) {
    return {
      nextStart: clickedYmd,
      nextEnd: tempStart,
      nextStartDisplay: clickedDisplay,
      nextEndDisplay: formatDateDisplay(tempStart),
    };
  }

  return {
    nextStart: tempStart,
    nextEnd: clickedYmd,
    nextStartDisplay: formatDateDisplay(tempStart),
    nextEndDisplay: clickedDisplay,
  };
}

/** Constrói o grid de semanas de um mês */
function generateMonthDaysGrid(currentMonth: Date) {
  const year = currentMonth.getFullYear();
  const month = currentMonth.getMonth();
  const firstDayOfMonth = new Date(year, month, 1);
  const startingDayOfWeek = firstDayOfMonth.getDay();
  const daysInMonth = new Date(year, month + 1, 0).getDate();
  const today = new Date();
  const todayMid = getMidnightTimestamp(today);

  const grid: { date: Date; isCurrentMonth: boolean; key: string }[] = [];

  const daysInPrevMonth = new Date(year, month, 0).getDate();
  for (let i = startingDayOfWeek - 1; i >= 0; i--) {
    const d = new Date(year, month - 1, daysInPrevMonth - i);
    grid.push({ date: d, isCurrentMonth: false, key: formatYmd(d) });
  }

  for (let day = 1; day <= daysInMonth; day++) {
    const d = new Date(year, month, day);
    grid.push({ date: d, isCurrentMonth: true, key: formatYmd(d) });
  }

  const remainingCells = (7 - (grid.length % 7)) % 7;
  for (let i = 1; i <= remainingCells; i++) {
    const d = new Date(year, month + 1, i);
    grid.push({ date: d, isCurrentMonth: false, key: formatYmd(d) });
  }

  return { daysGrid: grid, todayTimestamp: todayMid };
}

const CalendarDayCell = React.memo<CalendarDayCellProps>(({
  dayDate,
  isCurrentMonth,
  tempStart,
  tempEnd,
  hoveredDate,
  todayTimestamp,
  onDayClick,
  onDayHover,
}) => {
  const dayTime = getMidnightTimestamp(dayDate);
  const startD = parseYmdToDate(tempStart);
  const endD = parseYmdToDate(tempEnd);

  const startTime = startD ? getMidnightTimestamp(startD) : null;
  const endTime = endD ? getMidnightTimestamp(endD) : null;
  const hoverTime = hoveredDate ? getMidnightTimestamp(hoveredDate) : null;

  const { isStart, isEnd, isInRange, isHoverRange } = getDayRangeState(
    dayTime,
    startTime,
    endTime,
    hoverTime
  );

  const isToday = dayTime === todayTimestamp;

  return (
    <div
      className={cn(
        'relative h-7 flex items-center justify-center',
        isInRange && 'bg-brand-light/60',
        isStart && (endTime !== null || (hoverTime !== null && hoverTime > (startTime ?? 0))) && 'rounded-l-lg bg-gradient-to-r from-transparent via-brand-light/60 to-brand-light/60',
        isEnd && startTime !== null && 'rounded-r-lg bg-gradient-to-l from-transparent via-brand-light/60 to-brand-light/60',
        isHoverRange && 'bg-brand-light/40'
      )}
    >
      <button
        type="button"
        onClick={() => onDayClick(dayDate)}
        onMouseEnter={() => onDayHover(dayDate)}
        onMouseLeave={() => onDayHover(null)}
        className={cn(
          'w-7 h-7 text-xs font-semibold rounded-lg transition-colors duration-100 flex items-center justify-center cursor-pointer relative z-10',
          !isCurrentMonth && 'text-slate-300 hover:text-slate-500',
          isCurrentMonth && !isStart && !isEnd && 'text-slate-700 hover:bg-slate-200/70 hover:text-slate-900',
          (isStart || isEnd) && 'bg-brand text-white font-bold hover:bg-brand-dark shadow-2xs',
          isToday && !isStart && !isEnd && 'ring-1 ring-brand/40 font-bold text-brand-dark'
        )}
      >
        {dayDate.getDate()}
      </button>
    </div>
  );
});

CalendarDayCell.displayName = 'CalendarDayCell';

const DateRangePickerComponent: React.FC<DateRangePickerProps> = ({
  startDate,
  endDate,
  isActive,
  onApply,
  className,
}) => {
  const isCustomActive = isActive !== undefined ? isActive : Boolean(startDate && endDate);

  const [isOpen, setIsOpen] = useState(false);
  const [tempStart, setTempStart] = useState<string | undefined>(isCustomActive ? startDate : undefined);
  const [tempEnd, setTempEnd] = useState<string | undefined>(isCustomActive ? endDate : undefined);

  const [startInputText, setStartInputText] = useState<string>(
    isCustomActive && startDate ? formatDateDisplay(startDate) : ''
  );
  const [endInputText, setEndInputText] = useState<string>(
    isCustomActive && endDate ? formatDateDisplay(endDate) : ''
  );

  const [currentMonth, setCurrentMonth] = useState<Date>(() => {
    if (isCustomActive && startDate) {
      const d = parseYmdToDate(startDate);
      if (d) return d;
    }
    return new Date();
  });
  const [hoveredDate, setHoveredDate] = useState<Date | null>(null);

  const handleClose = useCallback(() => setIsOpen(false), []);

  const { triggerRef, popoverRef, position } = useFloatingPopover({
    isOpen,
    onClose: handleClose,
    width: 284,
    height: 390,
    align: 'left',
  });

  const syncStateFromProps = useCallback(() => {
    if (isCustomActive) {
      setTempStart(startDate);
      setTempEnd(endDate);
      setStartInputText(startDate ? formatDateDisplay(startDate) : '');
      setEndInputText(endDate ? formatDateDisplay(endDate) : '');
      if (startDate) {
        const d = parseYmdToDate(startDate);
        if (d) setCurrentMonth(d);
      }
    } else {
      setTempStart(undefined);
      setTempEnd(undefined);
      setStartInputText('');
      setEndInputText('');
      setCurrentMonth(new Date());
    }
  }, [isCustomActive, startDate, endDate]);

  useEffect(() => {
    if (isOpen) {
      syncStateFromProps();
    }
  }, [isOpen, syncStateFromProps]);

  const handlePrevMonth = () => {
    setCurrentMonth((prev) => new Date(prev.getFullYear(), prev.getMonth() - 1, 1));
  };

  const handleNextMonth = () => {
    setCurrentMonth((prev) => new Date(prev.getFullYear(), prev.getMonth() + 1, 1));
  };

  const handleDayClick = useCallback((dayDate: Date) => {
    const { nextStart, nextEnd, nextStartDisplay, nextEndDisplay } = computeNextDateRange(
      dayDate,
      tempStart,
      tempEnd
    );
    setTempStart(nextStart);
    setTempEnd(nextEnd);
    setStartInputText(nextStartDisplay);
    setEndInputText(nextEndDisplay);
  }, [tempStart, tempEnd]);

  const handleStartInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const formatted = formatDisplayDateInput(e.target.value);
    setStartInputText(formatted);
    const ymd = parseDisplayDateToYmd(formatted);
    if (ymd) {
      setTempStart(ymd);
      const d = parseYmdToDate(ymd);
      if (d) setCurrentMonth(d);
    } else if (formatted.length === 0) {
      setTempStart(undefined);
    }
  };

  const handleEndInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const formatted = formatDisplayDateInput(e.target.value);
    setEndInputText(formatted);
    const ymd = parseDisplayDateToYmd(formatted);
    if (ymd) {
      setTempEnd(ymd);
    } else if (formatted.length === 0) {
      setTempEnd(undefined);
    }
  };

  const handleApplyClick = () => {
    onApply({
      startDate: tempStart,
      endDate: tempEnd,
    });
    setIsOpen(false);
  };

  const handleClear = () => {
    setTempStart(undefined);
    setTempEnd(undefined);
    setStartInputText('');
    setEndInputText('');
  };

  const monthYearLabel = useMemo(() => {
    const label = currentMonth.toLocaleDateString('pt-BR', {
      month: 'long',
      year: 'numeric',
    });
    return label.charAt(0).toUpperCase() + label.slice(1);
  }, [currentMonth]);

  const { daysGrid, todayTimestamp } = useMemo(() => {
    return generateMonthDaysGrid(currentMonth);
  }, [currentMonth]);

  const triggerLabel = isCustomActive && startDate && endDate
    ? `${formatDateDisplay(startDate)} - ${formatDateDisplay(endDate)}`
    : 'Personalizado';

  return (
    <div
      className={cn('relative inline-block', className)}
      ref={triggerRef as React.RefObject<HTMLDivElement>}
    >
      {/* Botão Trigger */}
      <button
        type="button"
        onClick={() => setIsOpen((prev) => !prev)}
        aria-pressed={isOpen || isCustomActive}
        className={cn(
          'px-3 py-1.5 rounded-xl text-xs font-semibold border transition-colors duration-150 cursor-pointer select-none inline-flex items-center gap-1.5',
          isCustomActive
            ? 'bg-brand-light text-brand-dark border-brand font-bold shadow-2xs ring-1 ring-brand/20'
            : 'bg-surface-ground text-slate-600 border-border-subtle hover:bg-slate-200/60 hover:text-slate-800'
        )}
      >
        <CalendarIcon className="w-3.5 h-3.5 text-brand shrink-0" />
        <span>{triggerLabel}</span>
      </button>

      {isOpen &&
        position &&
        ReactDOM.createPortal(
          <div
            ref={popoverRef as React.RefObject<HTMLDivElement>}
            style={{
              position: 'fixed',
              top: `${position.top}px`,
              left: `${position.left}px`,
            }}
            className="z-[9999] w-[284px] p-3 bg-surface-card rounded-2xl shadow-elevated border border-border-subtle flex flex-col gap-2.5 select-none text-slate-800"
          >
            {/* Cabeçalho Compacto do Popover */}
            <div className="flex items-center justify-between pb-2 border-b border-border-subtle/80">
              <div className="flex items-center gap-1.5">
                <CalendarIcon className="w-3.5 h-3.5 text-brand" />
                <span className="text-xs font-bold text-slate-800">Selecione o Período</span>
              </div>
              <button
                type="button"
                onClick={handleClose}
                className="p-1 rounded-lg text-slate-400 hover:text-slate-600 hover:bg-slate-100 transition-colors cursor-pointer"
                aria-label="Fechar calendário"
              >
                <X className="w-3.5 h-3.5" />
              </button>
            </div>

            {/* Inputs de Texto DD/MM/AAAA */}
            <div className="grid grid-cols-2 gap-2 pb-1">
              <div className="flex flex-col gap-0.5">
                <label htmlFor="custom-date-start-input" className="text-[10px] font-bold text-slate-500 uppercase tracking-wider pl-0.5">
                  De
                </label>
                <input
                  id="custom-date-start-input"
                  aria-label="Data inicial"
                  type="text"
                  inputMode="numeric"
                  placeholder="DD/MM/AAAA"
                  maxLength={10}
                  value={startInputText}
                  onChange={handleStartInputChange}
                  className="w-full h-7 px-2 text-[11px] font-medium rounded-lg border border-border-subtle bg-surface-ground text-slate-800 placeholder:text-slate-400 focus:outline-none focus:border-brand focus:ring-1 focus:ring-brand/20 transition-colors text-center"
                />
              </div>
              <div className="flex flex-col gap-0.5">
                <label htmlFor="custom-date-end-input" className="text-[10px] font-bold text-slate-500 uppercase tracking-wider pl-0.5">
                  Até
                </label>
                <input
                  id="custom-date-end-input"
                  aria-label="Data final"
                  type="text"
                  inputMode="numeric"
                  placeholder="DD/MM/AAAA"
                  maxLength={10}
                  value={endInputText}
                  onChange={handleEndInputChange}
                  className="w-full h-7 px-2 text-[11px] font-medium rounded-lg border border-border-subtle bg-surface-ground text-slate-800 placeholder:text-slate-400 focus:outline-none focus:border-brand focus:ring-1 focus:ring-brand/20 transition-colors text-center"
                />
              </div>
            </div>

            {/* Navegação do Mês / Ano */}
            <div className="flex items-center justify-between px-1 pt-0.5 border-t border-border-subtle/60">
              <button
                type="button"
                onClick={handlePrevMonth}
                aria-label="Mês anterior"
                className="p-1 rounded-lg text-slate-600 hover:bg-slate-100 hover:text-slate-900 transition-colors cursor-pointer"
              >
                <ChevronLeft className="w-4 h-4" />
              </button>
              <span className="text-xs font-bold text-slate-800">{monthYearLabel}</span>
              <button
                type="button"
                onClick={handleNextMonth}
                aria-label="Próximo mês"
                className="p-1 rounded-lg text-slate-600 hover:bg-slate-100 hover:text-slate-900 transition-colors cursor-pointer"
              >
                <ChevronRight className="w-4 h-4" />
              </button>
            </div>

            {/* Cabeçalho dos Dias da Semana */}
            <div className="grid grid-cols-7 text-center text-[10px] font-bold text-slate-400 uppercase tracking-wider">
              <span>Dom</span>
              <span>Seg</span>
              <span>Ter</span>
              <span>Qua</span>
              <span>Qui</span>
              <span>Sex</span>
              <span>Sáb</span>
            </div>

            {/* Grade de Dias Compacta */}
            <div className="grid grid-cols-7 gap-y-0.5">
              {daysGrid.map(({ date: dayDate, isCurrentMonth, key }) => (
                <CalendarDayCell
                  key={key}
                  dayDate={dayDate}
                  isCurrentMonth={isCurrentMonth}
                  tempStart={tempStart}
                  tempEnd={tempEnd}
                  hoveredDate={hoveredDate}
                  todayTimestamp={todayTimestamp}
                  onDayClick={handleDayClick}
                  onDayHover={setHoveredDate}
                />
              ))}
            </div>

            {/* Rodapé de Ações Compacto */}
            <div className="flex items-center justify-between pt-2 border-t border-border-subtle/80 text-xs">
              <button
                type="button"
                onClick={handleClear}
                className="px-2 py-1 rounded-lg text-slate-500 hover:bg-slate-100 hover:text-slate-700 transition-colors cursor-pointer inline-flex items-center gap-1 text-[11px] font-semibold"
              >
                <RotateCcw className="w-3 h-3" />
                Limpar
              </button>

              <div className="flex items-center gap-1.5">
                <button
                  type="button"
                  onClick={handleClose}
                  className="px-2.5 py-1 rounded-lg text-slate-600 hover:bg-slate-100 hover:text-slate-800 transition-colors cursor-pointer font-medium text-[11px]"
                >
                  Cancelar
                </button>
                <button
                  type="button"
                  onClick={handleApplyClick}
                  className="px-3 py-1 rounded-lg font-bold bg-brand text-white hover:bg-brand-dark active:scale-[0.98] transition-colors cursor-pointer shadow-2xs inline-flex items-center gap-1 text-[11px]"
                >
                  <Check className="w-3 h-3" />
                  Aplicar
                </button>
              </div>
            </div>
          </div>,
          document.body
        )}
    </div>
  );
};

export const DateRangePicker = React.memo(DateRangePickerComponent);
