import React from 'react';
import { cn } from '@/shared/utils/cn';

export interface SwitchProps {
  checked: boolean;
  onChange: (checked: boolean) => void;
  disabled?: boolean;
  label?: string;
  description?: string;
  className?: string;
  id?: string;
}

export const Switch: React.FC<SwitchProps> = ({
  checked,
  onChange,
  disabled = false,
  label,
  description,
  className,
  id,
}) => {
  const generatedId = React.useId();
  const switchId = id ?? generatedId;

  return (
    <div className={cn('inline-flex items-center gap-2.5', className)}>
      <button
        id={switchId}
        type="button"
        role="switch"
        aria-checked={checked}
        disabled={disabled}
        onClick={() => !disabled && onChange(!checked)}
        className={cn(
          'relative inline-flex h-6 w-11 shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none focus:ring-2 focus:ring-brand focus:ring-offset-2',
          checked ? 'bg-brand' : 'bg-slate-300',
          disabled && 'opacity-50 cursor-not-allowed'
        )}
      >
        <span
          className={cn(
            'pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow-sm ring-0 transition duration-200 ease-in-out',
            checked ? 'translate-x-5' : 'translate-x-0'
          )}
        />
      </button>

      {(label || description) && (
        <label htmlFor={switchId} className="flex flex-col cursor-pointer">
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
