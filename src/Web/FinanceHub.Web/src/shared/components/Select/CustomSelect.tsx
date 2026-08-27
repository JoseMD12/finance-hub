import React, { useState, useRef, useEffect } from 'react';
import { cn } from '@/shared/utils/cn';
import { ChevronDown, Check, X } from 'lucide-react';

export interface SelectOption {
  value: string;
  label: string;
  icon?: React.ReactNode;
  badge?: string;
}

export interface CustomSelectProps {
  options: SelectOption[];
  value: string;
  onChange: (value: string) => void;
  label?: string;
  placeholder?: string;
  className?: string;
  disabled?: boolean;
  clearable?: boolean;
  size?: 'sm' | 'md';
  icon?: React.ReactNode;
  direction?: 'down' | 'up';
}

function getOptionStateClass(isSelected: boolean, isFocused: boolean): string {
  if (isSelected) return 'bg-brand-light text-brand-dark font-bold border-brand/20 shadow-2xs';
  if (isFocused) return 'bg-slate-100/80 text-slate-900 font-semibold';
  return 'hover:bg-slate-100/80 text-slate-700 hover:text-slate-900';
}

export const CustomSelect: React.FC<CustomSelectProps> = ({
  options,
  value,
  onChange,
  label,
  placeholder = 'Selecione uma opção',
  className,
  disabled = false,
  clearable = true,
  size = 'md',
  icon: defaultIcon,
  direction = 'down',
}) => {
  const [isOpen, setIsOpen] = useState(false);
  const [focusedIndex, setFocusedIndex] = useState(-1);
  const containerRef = useRef<HTMLDivElement>(null);

  const selectedOption = options.find((opt) => opt.value === value);

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };
    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside);
    }
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [isOpen]);

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (disabled) return;

    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      if (isOpen && focusedIndex >= 0 && focusedIndex < options.length) {
        onChange(options[focusedIndex].value);
        setIsOpen(false);
      } else {
        setIsOpen((prev) => !prev);
      }
    } else if (e.key === 'ArrowDown') {
      e.preventDefault();
      if (!isOpen) {
        setIsOpen(true);
        setFocusedIndex(0);
      } else {
        setFocusedIndex((prev) => (prev + 1) % options.length);
      }
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      if (!isOpen) {
        setIsOpen(true);
        setFocusedIndex(options.length - 1);
      } else {
        setFocusedIndex((prev) => (prev - 1 + options.length) % options.length);
      }
    } else if (e.key === 'Escape') {
      setIsOpen(false);
    }
  };

  const handleClear = (e: React.MouseEvent) => {
    e.stopPropagation();
    onChange('');
    setIsOpen(false);
  };

  const isSmall = size === 'sm';
  const displayIcon = selectedOption?.icon ?? defaultIcon;

  return (
    <div className={cn('flex flex-col gap-1.5 w-full relative', className)} ref={containerRef}>
      {label && <label className="text-xs font-semibold text-slate-700 pl-1">{label}</label>}

      <div className={cn('relative w-full', isOpen && 'z-50')}>
        <button
          type="button"
          disabled={disabled}
          onClick={() => setIsOpen((prev) => !prev)}
          onKeyDown={handleKeyDown}
          aria-haspopup="listbox"
          aria-expanded={isOpen}
          aria-label={label || placeholder}
          className={cn(
            'flex items-center justify-between w-full text-xs font-medium bg-surface-ground border border-border-subtle rounded-xl cursor-pointer transition-colors duration-150 outline-none select-none disabled:opacity-50 disabled:cursor-not-allowed form-input-focus',
            isSmall ? 'h-9 px-3 py-1.5' : 'h-10 px-4 py-2',
            isOpen
              ? 'border-brand bg-surface-card ring-2 ring-brand/20 shadow-sm'
              : 'hover:border-slate-300'
          )}
        >
          <span className="flex items-center gap-2 truncate">
            {displayIcon && <span className="shrink-0">{displayIcon}</span>}
            <span
              className={cn(
                'truncate',
                selectedOption && selectedOption.value !== ''
                  ? 'text-slate-800 font-bold'
                  : 'text-slate-600 font-medium'
              )}
            >
              {selectedOption ? selectedOption.label : placeholder}
            </span>
          </span>

          <div className="flex items-center gap-1.5 shrink-0">
            {clearable && selectedOption && selectedOption.value !== '' && (
              <button
                type="button"
                onClick={handleClear}
                aria-label="Limpar seleção"
                className="p-1 rounded-md text-slate-400 hover:text-slate-600 hover:bg-slate-200/60 transition-colors cursor-pointer"
              >
                <X className="w-3.5 h-3.5" />
              </button>
            )}
            <ChevronDown
              className={cn(
                'w-4 h-4 text-slate-400 transition-transform duration-200',
                isOpen && 'rotate-180 text-brand'
              )}
            />
          </div>
        </button>

        {isOpen && (
          <div
            role="listbox"
            tabIndex={-1}
            className={cn(
              'absolute left-0 right-0 z-50 p-2 bg-surface-card border border-border-subtle rounded-2xl shadow-elevated flex flex-col gap-1 min-w-[140px] max-h-60 overflow-y-auto animate-in fade-in zoom-in-95 duration-150',
              direction === 'up' ? 'bottom-[calc(100%+6px)]' : 'top-[calc(100%+6px)]'
            )}
          >
            {options.map((option, idx) => {
              const isSelected = option.value === value;
              const isFocused = idx === focusedIndex;

              return (
                <button
                  key={option.value || `opt-${idx}`}
                  type="button"
                  role="option"
                  aria-selected={isSelected}
                  onClick={() => {
                    onChange(option.value);
                    setIsOpen(false);
                  }}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter' || e.key === ' ') {
                      e.preventDefault();
                      onChange(option.value);
                      setIsOpen(false);
                    }
                  }}
                  onMouseEnter={() => setFocusedIndex(idx)}
                  className={cn(
                    'flex items-center justify-between w-full px-3 py-2 text-xs font-medium rounded-xl cursor-pointer transition-colors duration-150 outline-none text-left border border-transparent',
                    getOptionStateClass(isSelected, isFocused)
                  )}
                >
                  <span className="flex items-center gap-2.5 truncate">
                    {option.icon && <span className="shrink-0">{option.icon}</span>}
                    <span className="truncate">{option.label}</span>
                  </span>

                  <div className="flex items-center gap-1.5 shrink-0 ml-2">
                    {option.badge && (
                      <span className="px-1.5 py-0.5 rounded-md text-[10px] font-semibold bg-surface-ground border border-border-subtle text-slate-600">
                        {option.badge}
                      </span>
                    )}
                    {isSelected && <Check className="w-3.5 h-3.5 text-brand shrink-0" />}
                  </div>
                </button>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
};
