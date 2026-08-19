import type { LucideIcon } from 'lucide-react';
import type { ReactNode } from 'react';
import { cn } from '@/shared/utils/cn';

type StatusBadgeTone = 'brand' | 'secondary' | 'success' | 'danger' | 'warning' | 'info' | 'muted';

const toneClasses: Record<StatusBadgeTone, string> = {
  brand: 'bg-brand-light text-brand-dark',
  secondary: 'bg-secondary-light text-secondary-dark',
  success: 'bg-status-success-bg text-status-success',
  danger: 'bg-status-danger-bg text-status-danger',
  warning: 'bg-status-warning-bg text-status-warning',
  info: 'bg-status-info-bg text-status-info',
  muted: 'bg-surface-muted text-slate-600',
};

interface StatusBadgeProps {
  readonly icon: LucideIcon;
  readonly children: ReactNode;
  readonly tone?: StatusBadgeTone;
  readonly className?: string;
}

export function StatusBadge({ icon: Icon, children, tone = 'secondary', className }: Readonly<StatusBadgeProps>) {
  return (
    <span
      className={cn(
        'inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-[11px] font-bold',
        toneClasses[tone],
        className
      )}
    >
      <Icon className="h-3.5 w-3.5 flex-shrink-0" aria-hidden="true" />
      <span>{children}</span>
    </span>
  );
}
