import React, { useCallback } from 'react';
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
  const hasGlow = Boolean(glowRgb && !prefersReduced);

  const handleMouseMove = useCallback((e: React.MouseEvent<HTMLDivElement>) => {
    if (hasGlow) {
      // Usa offsetX/offsetY nativos do evento sem chamar getBoundingClientRect() (zero layout reflow)
      const target = e.currentTarget;
      const x = (e.nativeEvent as MouseEvent).offsetX;
      const y = (e.nativeEvent as MouseEvent).offsetY;
      target.style.setProperty('--mouse-x', `${x}px`);
      target.style.setProperty('--mouse-y', `${y}px`);
    }
    onMouseMove?.(e);
  }, [hasGlow, onMouseMove]);

  const variantStyles = {
    default: 'bg-surface-card border border-border-subtle shadow-card',
    elevated: 'bg-surface-card border border-border-subtle shadow-elevated',
    muted: 'bg-surface-muted border border-border-subtle',
  };

  return (
    <div
      onMouseMove={hasGlow ? handleMouseMove : onMouseMove}
      style={{
        ...style,
        ...(glowRgb ? { ['--glow-rgb' as string]: glowRgb } : {}),
      }}
      className={cn(
        'relative rounded-2xl p-6 transition-[box-shadow] duration-200 block',
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
