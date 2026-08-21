import React, { type ReactNode, type ElementType, type ComponentPropsWithoutRef } from 'react';
import { useReducedMotion } from 'framer-motion';
import { cn } from '../../utils/cn';

export interface GlowCardProps<T extends ElementType = 'div'> {
  children: ReactNode;
  className?: string;
  glowRgb?: string; // e.g. '224, 86, 151' or '29, 85, 90'
  as?: T;
}

export const GlowCard = <T extends ElementType = 'div'>({
  children,
  className,
  glowRgb = '224, 86, 151',
  as,
  style,
  ...rest
}: GlowCardProps<T> & Omit<ComponentPropsWithoutRef<T>, keyof GlowCardProps<T>>) => {
  const Component = as || 'div';
  const prefersReduced = useReducedMotion();

  const handleMouseMove = (e: React.MouseEvent<HTMLElement>) => {
    if (prefersReduced) return;
    const rect = e.currentTarget.getBoundingClientRect();
    e.currentTarget.style.setProperty('--mouse-x', `${e.clientX - rect.left}px`);
    e.currentTarget.style.setProperty('--mouse-y', `${e.clientY - rect.top}px`);
  };

  return (
    <Component
      onMouseMove={handleMouseMove}
      style={{
        ...style,
        ['--glow-rgb' as string]: glowRgb,
      }}
      className={cn('relative rounded-2xl block', !prefersReduced && 'glow-card', className)}
      {...rest}
    >
      {children}
    </Component>
  );
};




