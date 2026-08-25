import React from 'react';
import { useReducedMotion } from 'framer-motion';
import { cn } from '@/shared/utils/cn';

export interface CardProps extends React.HTMLAttributes<HTMLDivElement> {
  variant?: 'default' | 'elevated' | 'muted';
  hoverable?: boolean;
  glowRgb?: string; // Ex: '224, 86, 151' ou '46, 204, 113'
}

export const Card: React.FC<CardProps> = ({
  className,
  variant = 'default',
  hoverable = true,
  glowRgb,
  style,
  onMouseMove,
  children,
  ...props
}) => {
  const prefersReduced = useReducedMotion();

  const handleMouseMove = (e: React.MouseEvent<HTMLDivElement>) => {
    if (!prefersReduced && glowRgb) {
      const rect = e.currentTarget.getBoundingClientRect();
      e.currentTarget.style.setProperty('--mouse-x', `${e.clientX - rect.left}px`);
      e.currentTarget.style.setProperty('--mouse-y', `${e.clientY - rect.top}px`);
    }
    onMouseMove?.(e);
  };

  const variantStyles = {
    default: 'bg-surface-card border border-border-subtle shadow-card',
    elevated: 'bg-surface-card border border-border-subtle shadow-elevated',
    muted: 'bg-surface-muted border border-border-subtle',
  };

  const hasGlow = Boolean(glowRgb && !prefersReduced);

  return (
    <div
      onMouseMove={handleMouseMove}
      style={{
        ...style,
        ...(glowRgb ? { ['--glow-rgb' as string]: glowRgb } : {}),
      }}
      className={cn(
        'relative rounded-2xl p-6 transition-[box-shadow,transform] duration-200 block',
        variantStyles[variant],
        hoverable && 'hover:shadow-elevated hover:-translate-y-0.5',
        hasGlow && 'glow-card',
        className
      )}
      {...props}
    >
      {children}
    </div>
  );
};


