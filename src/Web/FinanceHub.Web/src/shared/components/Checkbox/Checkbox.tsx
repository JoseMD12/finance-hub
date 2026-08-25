import React from 'react';
import { Check } from 'lucide-react';
import { cn } from '@/shared/utils/cn';

export interface CheckboxProps extends Omit<React.InputHTMLAttributes<HTMLInputElement>, 'onChange'> {
  checked: boolean;
  onChange: (checked: boolean) => void;
  label?: React.ReactNode;
  description?: React.ReactNode;
  className?: string;
}

export const Checkbox: React.FC<CheckboxProps> = ({
  checked,
  onChange,
  disabled = false,
  label,
  description,
  className,
  id,
  ...props
}) => {
  const generatedId = React.useId();
  const checkboxId = id || generatedId;

  return (
    <div className={cn('inline-flex items-start gap-2 select-none', className)}>
      <div className="relative flex items-center h-5">
        <input
          id={checkboxId}
          type="checkbox"
          checked={checked}
          disabled={disabled}
          onChange={(e) => !disabled && onChange(e.target.checked)}
          className={cn(
            'peer appearance-none w-4 h-4 rounded border transition-all duration-150 cursor-pointer focus:outline-none focus:ring-2 focus:ring-brand/30',
            'bg-surface-card border-border-subtle checked:bg-brand checked:border-brand',
            disabled && 'opacity-50 cursor-not-allowed'
          )}
          {...props}
        />
        <Check className={cn('pointer-events-none absolute left-0.5 top-0.5 w-3 h-3 stroke-[3] text-white opacity-0 peer-checked:opacity-100 transition-opacity duration-150')} />
      </div>

      {(label || description) && (
        <label htmlFor={checkboxId} className={cn('flex flex-col cursor-pointer', disabled && 'cursor-not-allowed opacity-50')}>
          {label && (
            <span className="text-xs font-semibold text-slate-700 hover:text-slate-900 transition-colors">
              {label}
            </span>
          )}
          {description && (
            <span className="text-[11px] text-slate-400 font-medium">
              {description}
            </span>
          )}
        </label>
      )}
    </div>
  );
};
