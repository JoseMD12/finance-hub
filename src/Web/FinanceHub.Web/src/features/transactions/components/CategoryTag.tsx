import React from 'react';
import { cn } from '@/shared/utils/cn';
import {
  Utensils,
  Car,
  Home,
  HeartPulse,
  Tv,
  ShoppingBag,
  GraduationCap,
  Landmark,
  TrendingUp,
  Tag,
  type LucideIcon,
} from 'lucide-react';

const iconMap: Record<string, LucideIcon> = {
  utensils: Utensils,
  car: Car,
  home: Home,
  'heart-pulse': HeartPulse,
  tv: Tv,
  'shopping-bag': ShoppingBag,
  'graduation-cap': GraduationCap,
  landmark: Landmark,
  'trending-up': TrendingUp,
  tag: Tag,
};

const colorStyleMap: Record<string, { bg: string; text: string; border: string }> = {
  emerald: { bg: 'bg-emerald-50', text: 'text-emerald-700', border: 'border-emerald-200' },
  sky: { bg: 'bg-sky-50', text: 'text-sky-700', border: 'border-sky-200' },
  amber: { bg: 'bg-amber-50', text: 'text-amber-700', border: 'border-amber-200' },
  rose: { bg: 'bg-rose-50', text: 'text-rose-700', border: 'border-rose-200' },
  purple: { bg: 'bg-purple-50', text: 'text-purple-700', border: 'border-purple-200' },
  indigo: { bg: 'bg-indigo-50', text: 'text-indigo-700', border: 'border-indigo-200' },
  teal: { bg: 'bg-teal-50', text: 'text-teal-700', border: 'border-teal-200' },
  blue: { bg: 'bg-blue-50', text: 'text-blue-700', border: 'border-blue-200' },
  green: { bg: 'bg-green-50', text: 'text-green-700', border: 'border-green-200' },
  gray: { bg: 'bg-slate-100', text: 'text-slate-700', border: 'border-slate-200' },
};

export interface CategoryTagProps {
  name: string;
  iconKey?: string;
  colorToken?: string;
  onClick?: () => void;
  interactive?: boolean;
}

export const CategoryTag: React.FC<CategoryTagProps> = ({
  name,
  iconKey = 'tag',
  colorToken = 'gray',
  onClick,
  interactive = false,
}) => {
  const Icon = iconMap[iconKey] || Tag;
  const style = colorStyleMap[colorToken] || colorStyleMap.gray;

  return (
    <button
      type="button"
      onClick={onClick}
      disabled={!interactive}
      aria-label={`Categoria: ${name}`}
      className={cn(
        'inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md text-xs font-semibold border transition-all duration-150 whitespace-nowrap shrink-0',
        style.bg,
        style.text,
        style.border,
        interactive && 'cursor-pointer hover:shadow-sm hover:brightness-95 active:scale-95'
      )}
    >
      <Icon className="w-3.5 h-3.5 shrink-0" aria-hidden="true" />
      <span className="whitespace-nowrap">{name}</span>
    </button>
  );
};
