import React, { useCallback, useRef, useEffect } from 'react';
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
  onMouseLeave,
  children,
  ...props
}) => {
  const prefersReduced = useReducedMotion();
  const hasGlow = Boolean(glowRgb && !prefersReduced);
  const rafRef = useRef<number | null>(null);

  const handleMouseMove = useCallback((e: React.MouseEvent<HTMLDivElement>) => {
    if (hasGlow) {
      const target = e.currentTarget;
      const clientX = e.clientX;
      const clientY = e.clientY;

      if (rafRef.current !== null) {
        cancelAnimationFrame(rafRef.current);
      }

      rafRef.current = requestAnimationFrame(() => {
        const rect = target.getBoundingClientRect();
        target.style.setProperty('--mouse-x', `${clientX - rect.left}px`);
        target.style.setProperty('--mouse-y', `${clientY - rect.top}px`);
        rafRef.current = null;
      });
    }
    onMouseMove?.(e);
  }, [hasGlow, onMouseMove]);

  const handleMouseLeave = useCallback((e: React.MouseEvent<HTMLDivElement>) => {
    if (hasGlow) {
      if (rafRef.current !== null) {
        cancelAnimationFrame(rafRef.current);
        rafRef.current = null;
      }
      e.currentTarget.style.setProperty('--mouse-x', '-999px');
      e.currentTarget.style.setProperty('--mouse-y', '-999px');
    }
    onMouseLeave?.(e);
  }, [hasGlow, onMouseLeave]);

  useEffect(() => {
    return () => {
      if (rafRef.current !== null) {
        cancelAnimationFrame(rafRef.current);
      }
    };
  }, []);

  const variantStyles = {
    default: 'bg-surface-card border border-border-subtle shadow-card',
    elevated: 'bg-surface-card border border-border-subtle shadow-elevated',
    muted: 'bg-surface-muted border border-border-subtle',
  };

  return (
    <div
      onMouseMove={hasGlow ? handleMouseMove : onMouseMove}
      onMouseLeave={hasGlow ? handleMouseLeave : onMouseLeave}
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
