import React from 'react';
import { cn } from '@/shared/utils/cn';

export interface CardProps extends React.HTMLAttributes<HTMLDivElement> {
  variant?: 'default' | 'elevated' | 'muted';
  hoverable?: boolean;
}

export const Card: React.FC<CardProps> = ({
  className,
  variant = 'default',
  hoverable = true,
  children,
  ...props
}) => {
  const variantStyles = {
    default: 'bg-surface-card border border-border-subtle shadow-card',
    elevated: 'bg-surface-card border border-border-subtle shadow-elevated',
    muted: 'bg-surface-muted border border-border-subtle',
  };

  return (
    <div
      className={cn(
        'rounded-2xl p-6 transition-[box-shadow,transform] duration-200',
        variantStyles[variant],
        hoverable && 'hover:shadow-elevated hover:-translate-y-0.5',
        className
      )}
      {...props}
    >
      {children}
    </div>
  );
};
