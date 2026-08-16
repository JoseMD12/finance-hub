import React from 'react';
import { cn } from '@/shared/utils/cn';

export interface CardProps extends React.HTMLAttributes<HTMLDivElement> {
  variant?: 'default' | 'elevated' | 'muted';
}

export const Card: React.FC<CardProps> = ({ className, variant = 'default', children, ...props }) => {
  const variantStyles = {
    default: 'bg-white border border-border-subtle shadow-card',
    elevated: 'bg-white border border-border-subtle shadow-elevated',
    muted: 'bg-surface-muted border border-border-subtle',
  };

  return (
    <div
      className={cn('rounded-2xl p-6 transition-all duration-200', variantStyles[variant], className)}
      {...props}
    >
      {children}
    </div>
  );
};
