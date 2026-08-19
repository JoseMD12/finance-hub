import type { LucideIcon } from 'lucide-react';
import { cn } from '@/shared/utils/cn';

type IconCircleTone = 'brand' | 'secondary' | 'tertiary' | 'success' | 'danger' | 'warning' | 'info' | 'muted';
type IconCircleSize = 'sm' | 'md' | 'lg';

const toneClasses: Record<IconCircleTone, string> = {
  brand: 'bg-brand-light text-brand',
  secondary: 'bg-secondary-light text-secondary',
  tertiary: 'bg-tertiary-light text-tertiary',
  success: 'bg-status-success-bg text-status-success',
  danger: 'bg-status-danger-bg text-status-danger',
  warning: 'bg-status-warning-bg text-status-warning',
  info: 'bg-status-info-bg text-status-info',
  muted: 'bg-surface-muted text-slate-500',
};

const sizeClasses: Record<IconCircleSize, string> = {
  sm: 'h-7 w-7 rounded-lg',
  md: 'h-8 w-8 rounded-xl',
  lg: 'h-10 w-10 rounded-xl',
};

const iconSizeClasses: Record<IconCircleSize, string> = {
  sm: 'h-3.5 w-3.5',
  md: 'h-4 w-4',
  lg: 'h-5 w-5',
};

interface IconCircleProps {
  readonly icon: LucideIcon;
  readonly tone?: IconCircleTone;
  readonly size?: IconCircleSize;
  readonly className?: string;
  readonly label?: string;
}

export function IconCircle({
  icon: Icon,
  tone = 'secondary',
  size = 'md',
  className,
  label,
}: Readonly<IconCircleProps>) {
  return (
    <span
      className={cn(
        'inline-flex flex-shrink-0 items-center justify-center shadow-sm',
        toneClasses[tone],
        sizeClasses[size],
        className
      )}
      aria-label={label}
    >
      <Icon className={iconSizeClasses[size]} aria-hidden={!label} />
    </span>
  );
}
