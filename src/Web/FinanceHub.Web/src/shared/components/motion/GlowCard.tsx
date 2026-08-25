import React, { type ReactNode } from 'react';
import { Card, type CardProps } from '../Card/Card';

export interface GlowCardProps extends CardProps {
  children: ReactNode;
  className?: string;
  glowRgb?: string; // e.g. '224, 86, 151' or '29, 85, 90'
}

export const GlowCard: React.FC<GlowCardProps> = ({
  children,
  className,
  glowRgb = '224, 86, 151',
  ...rest
}) => {
  return (
    <Card glowRgb={glowRgb} className={className} {...rest}>
      {children}
    </Card>
  );
};




