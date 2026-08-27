import React from 'react';
import { cn } from '@/shared/utils/cn';
import { getCategoryIcon } from '../utils/categoryIcons';

const colorStyleMap: Record<string, { bg: string; text: string; border: string; hoverBg: string }> = {
  emerald: { bg: 'bg-emerald-50', text: 'text-emerald-700', border: 'border-emerald-200', hoverBg: 'hover:bg-emerald-100' },
  sky: { bg: 'bg-sky-50', text: 'text-sky-700', border: 'border-sky-200', hoverBg: 'hover:bg-sky-100' },
  amber: { bg: 'bg-amber-50', text: 'text-amber-700', border: 'border-amber-200', hoverBg: 'hover:bg-amber-100' },
  rose: { bg: 'bg-rose-50', text: 'text-rose-700', border: 'border-rose-200', hoverBg: 'hover:bg-rose-100' },
  purple: { bg: 'bg-purple-50', text: 'text-purple-700', border: 'border-purple-200', hoverBg: 'hover:bg-purple-100' },
  indigo: { bg: 'bg-indigo-50', text: 'text-indigo-700', border: 'border-indigo-200', hoverBg: 'hover:bg-indigo-100' },
  teal: { bg: 'bg-teal-50', text: 'text-teal-700', border: 'border-teal-200', hoverBg: 'hover:bg-teal-100' },
  blue: { bg: 'bg-blue-50', text: 'text-blue-700', border: 'border-blue-200', hoverBg: 'hover:bg-blue-100' },
  green: { bg: 'bg-green-50', text: 'text-green-700', border: 'border-green-200', hoverBg: 'hover:bg-green-100' },
  gray: { bg: 'bg-slate-100', text: 'text-slate-700', border: 'border-slate-200', hoverBg: 'hover:bg-slate-200' },
};

export interface CategoryTagProps {
  name: string;
  iconKey?: string;
  colorToken?: string;
  onClick?: () => void;
  interactive?: boolean;
}

export const CategoryTagComponent: React.FC<CategoryTagProps> = ({
  name,
  iconKey = 'tag',
  colorToken = 'gray',
  onClick,
  interactive = false,
}) => {
  const icon = getCategoryIcon(iconKey);
  const style = colorStyleMap[colorToken] || colorStyleMap.gray;

  return (
    <button
      type="button"
      onClick={onClick}
      disabled={!interactive}
      aria-label={`Categoria: ${name}`}
      className={cn(
        'inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md text-xs font-semibold border whitespace-nowrap shrink-0',
        style.bg,
        style.text,
        style.border,
        interactive && ['cursor-pointer', style.hoverBg]
      )}
    >
      {React.createElement(icon, { className: 'w-3.5 h-3.5 shrink-0', 'aria-hidden': true })}
      <span className="whitespace-nowrap">{name}</span>
    </button>
  );
};

export const CategoryTag = React.memo(CategoryTagComponent);

