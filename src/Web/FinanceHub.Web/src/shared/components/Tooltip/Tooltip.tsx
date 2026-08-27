import React, { useState, useRef, useCallback, useEffect } from 'react';
import ReactDOM from 'react-dom';
import { cn } from '@/shared/utils/cn';

export interface TooltipProps {
  content: string;
  children: React.ReactNode;
  className?: string;
  position?: 'top' | 'bottom' | 'left' | 'right';
}

const GAP = 6;

export const Tooltip: React.FC<TooltipProps> = ({
  content,
  children,
  className,
  position = 'top',
}) => {
  const [isVisible, setIsVisible] = useState(false);
  const [coords, setCoords] = useState<{ top: number; left: number } | null>(null);
  const triggerRef = useRef<HTMLDivElement>(null);

  const updatePosition = useCallback(() => {
    const trigger = triggerRef.current;
    if (!trigger) return;
    const rect = trigger.getBoundingClientRect();

    switch (position) {
      case 'right':
        setCoords({ top: rect.top + rect.height / 2, left: rect.right + GAP });
        break;
      case 'left':
        setCoords({ top: rect.top + rect.height / 2, left: rect.left - GAP });
        break;
      case 'bottom':
        setCoords({ top: rect.bottom + GAP, left: rect.left + rect.width / 2 });
        break;
      case 'top':
      default:
        setCoords({ top: rect.top - GAP, left: rect.left + rect.width / 2 });
        break;
    }
  }, [position]);

  const show = useCallback(() => {
    updatePosition();
    setIsVisible(true);
  }, [updatePosition]);

  const hide = useCallback(() => setIsVisible(false), []);

  useEffect(() => {
    if (!isVisible) return;
    // Rola a página e some — evita um tooltip "fantasma" flutuando fora do trigger.
    window.addEventListener('scroll', hide, { passive: true, capture: true });
    return () => window.removeEventListener('scroll', hide, { capture: true } as EventListenerOptions);
  }, [isVisible, hide]);

  const getAnchorClasses = () => {
    switch (position) {
      case 'right':
        return '-translate-y-1/2 text-left';
      case 'left':
        return '-translate-x-full -translate-y-1/2 text-right';
      case 'bottom':
        return '-translate-x-1/2 text-center';
      case 'top':
      default:
        return '-translate-x-1/2 -translate-y-full text-center';
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
      ref={triggerRef}
      className={cn('inline-flex items-center', className)}
      onMouseEnter={show}
      onMouseLeave={hide}
      onFocus={show}
      onBlur={hide}
    >
      {children}
      {isVisible &&
        coords &&
        ReactDOM.createPortal(
          <div
            role="tooltip"
            style={{ position: 'fixed', top: `${coords.top}px`, left: `${coords.left}px` }}
            className={cn(
              'z-[9999] px-2.5 py-1.5 text-[11px] font-medium leading-snug',
              'bg-slate-900/95 text-slate-100 rounded-lg shadow-elevated border border-slate-700/80',
              'whitespace-normal min-w-[190px] max-w-[240px] pointer-events-none transition-all duration-150',
              getAnchorClasses()
            )}
          >
            {content}
            <div
              className={cn(
                'absolute w-2 h-2 bg-slate-900 border-slate-700/80 rotate-45 pointer-events-none',
                getArrowClasses()
              )}
            />
          </div>,
          document.body
        )}
    </div>
  );
};
