import React, { useState, useRef, useEffect } from 'react';
import { cn } from '@/shared/utils/cn';
import { ChevronDown, Check, Landmark } from 'lucide-react';
import { StatusBadge } from '@/shared/components/StatusBadge/StatusBadge';

export interface SelectOption {
  value: string;
  label: string;
  badge?: string;
  icon?: React.ReactNode;
}

export interface CustomSelectProps {
  options: SelectOption[];
  value: string;
  onChange: (value: string) => void;
  label?: string;
  placeholder?: string;
  className?: string;
  disabled?: boolean;
}

export const CustomSelect: React.FC<CustomSelectProps> = ({
  options,
  value,
  onChange,
  label,
  placeholder = 'Selecione uma opção',
  className,
  disabled = false,
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
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

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

  const getOptionStyle = (isSelected: boolean, isFocused: boolean) => {
    if (isSelected) {
      return 'bg-brand-light text-brand-dark font-bold';
    }
    if (isFocused) {
      return 'bg-secondary-light text-secondary-dark font-semibold';
    }
    return 'text-slate-700 hover:bg-secondary-light hover:text-secondary-dark';
  };

  return (
    <div className={cn('flex flex-col gap-1.5 w-full', className)} ref={containerRef}>
      {label && <label className="text-xs font-semibold text-slate-700">{label}</label>}
      <div className="relative w-full">
        <button
          type="button"
          disabled={disabled}
          onClick={() => setIsOpen((prev) => !prev)}
          onKeyDown={handleKeyDown}
          aria-haspopup="listbox"
          aria-expanded={isOpen}
          className={cn(
            'flex items-center justify-between w-full px-4 py-2.5 text-sm font-medium bg-surface-ground border border-border-subtle rounded-xl cursor-pointer transition-all duration-200 outline-none select-none disabled:opacity-50 disabled:cursor-not-allowed form-input-focus',
            isOpen ? 'border-brand bg-white ring-2 ring-brand/20 shadow-sm' : 'hover:border-slate-300'
          )}
        >
          <span className="flex items-center gap-2.5 truncate">
            {selectedOption?.icon}
            <span className={selectedOption ? 'text-slate-800 font-semibold' : 'text-slate-400'}>
              {selectedOption ? selectedOption.label : placeholder}
            </span>
          </span>
          <div className="flex items-center gap-2">
            {selectedOption?.badge && (
              <StatusBadge icon={Landmark} tone="secondary" className="px-2 py-0.5 text-[10px]">
                {selectedOption.badge}
              </StatusBadge>
            )}
            <ChevronDown className={cn('w-4 h-4 text-slate-400 transition-transform duration-200', isOpen && 'rotate-180 text-brand')} />
          </div>
        </button>

        {isOpen && (
          <div
            role="listbox"
            tabIndex={-1}
            className="absolute top-[calc(100%+6px)] left-0 right-0 z-50 p-1.5 bg-surface-card border border-border-subtle rounded-xl shadow-dropdown flex flex-col gap-1 max-h-60 overflow-y-auto"
          >
            {options.map((option, idx) => {
              const isSelected = option.value === value;
              const isFocused = idx === focusedIndex;
              return (
                <div
                  key={option.value}
                  role="option"
                  aria-selected={isSelected}
                  tabIndex={0}
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
                    'flex items-center justify-between px-3.5 py-2 text-xs font-medium rounded-lg cursor-pointer transition-colors duration-150 outline-none',
                    getOptionStyle(isSelected, isFocused)
                  )}
                >
                  <span className="flex items-center gap-2.5">
                    {option.icon}
                    {option.label}
                  </span>
                  <div className="flex items-center gap-2">
                    {option.badge && (
                      <StatusBadge icon={Landmark} tone="secondary" className="rounded-md px-1.5 py-0.5 text-[10px]">
                        {option.badge}
                      </StatusBadge>
                    )}
                    {isSelected && <Check className="w-3.5 h-3.5 text-brand" />}
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
};
