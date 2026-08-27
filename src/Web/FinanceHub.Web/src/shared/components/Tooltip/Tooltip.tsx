import React, { useState } from 'react';
import { cn } from '@/shared/utils/cn';

export interface TooltipProps {
  content: string;
  children: React.ReactNode;
  className?: string;
  position?: 'top' | 'bottom' | 'left' | 'right';
}

export const Tooltip: React.FC<TooltipProps> = ({
  content,
  children,
  className,
  position = 'top',
}) => {
  const [isVisible, setIsVisible] = useState(false);

  const getPositionClasses = () => {
    switch (position) {
      case 'right':
        return 'left-full ml-2 top-1/2 -translate-y-1/2 text-left';
      case 'left':
        return 'right-full mr-2 top-1/2 -translate-y-1/2 text-right';
      case 'bottom':
        return 'top-full mt-1.5 left-1/2 -translate-x-1/2 text-center';
      case 'top':
      default:
        return 'bottom-full mb-1.5 left-1/2 -translate-x-1/2 text-center';
    }
  };

  const getArrowClasses = () => {
    switch (position) {
      case 'right':
        return 'right-full top-1/2 -translate-y-1/2 -mr-1 border-l border-b';
      case 'left':
        return 'left-full top-1/2 -translate-y-1/2 -ml-1 border-r border-t';
      case 'bottom':
        return 'bottom-full left-1/2 -translate-x-1/2 -mb-1 border-l border-t';
      case 'top':
      default:
        return 'top-full left-1/2 -translate-x-1/2 -mt-1 border-r border-b';
    }
  };

  return (
    <div
      className={cn('relative inline-flex items-center', className)}
      onMouseEnter={() => setIsVisible(true)}
      onMouseLeave={() => setIsVisible(false)}
      onFocus={() => setIsVisible(true)}
      onBlur={() => setIsVisible(false)}
    >
      {children}
      {isVisible && (
        <div
          role="tooltip"
          className={cn(
            'absolute z-50 px-2.5 py-1.5 text-[11px] font-medium leading-snug',
            'bg-slate-900/95 text-slate-100 rounded-lg shadow-elevated border border-slate-700/80',
            'whitespace-normal min-w-[190px] max-w-[240px] pointer-events-none transition-all duration-150',
            getPositionClasses()
          )}
        >
          {content}
          <div
            className={cn(
              'absolute w-2 h-2 bg-slate-900 border-slate-700/80 rotate-45 pointer-events-none',
              getArrowClasses()
            )}
          />
        </div>
      )}
    </div>
  );
};
