import { useState, useRef, useEffect, useCallback, type RefObject } from 'react';

export interface FloatingPopoverConfig {
  isOpen: boolean;
  onClose: () => void;
  width: number;
  height: number;
  align?: 'left' | 'right';
  padding?: number;
  offset?: number;
  extraRef?: RefObject<HTMLElement | null>;
}

export interface FloatingCoords {
  top: number;
  left: number;
}

export function useFloatingPopover({
  isOpen,
  onClose,
  width,
  height,
  align = 'left',
  padding = 16,
  offset = 6,
  extraRef,
}: FloatingPopoverConfig) {
  const triggerRef = useRef<HTMLElement | null>(null);
  const popoverRef = useRef<HTMLElement | null>(null);
  const [position, setPosition] = useState<FloatingCoords | null>(null);

  const updatePosition = useCallback(() => {
    if (!triggerRef.current) return;
    const rect = triggerRef.current.getBoundingClientRect();

    let top = rect.bottom + offset;
    if (top + height > window.innerHeight && rect.top > height) {
      top = Math.max(padding, rect.top - height - offset);
    }

    let left = align === 'right' ? rect.right - width : rect.left;
    if (left + width > window.innerWidth - padding) {
      left = Math.max(padding, window.innerWidth - width - padding);
    }
    if (left < padding) {
      left = padding;
    }

    setPosition((prev) => {
      if (prev && Math.abs(prev.top - top) < 1 && Math.abs(prev.left - left) < 1) {
        return prev;
      }
      return { top, left };
    });
  }, [align, height, offset, padding, width]);

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      const target = event.target as Node;
      const insideTrigger = triggerRef.current?.contains(target);
      const insidePopover = popoverRef.current?.contains(target);
      const insideExtra = extraRef?.current?.contains(target);

      if (!insideTrigger && !insidePopover && !insideExtra) {
        onClose();
      }
    };

    if (isOpen) {
      updatePosition();
      document.addEventListener('mousedown', handleClickOutside);
      window.addEventListener('scroll', updatePosition, { passive: true, capture: true });
      window.addEventListener('resize', updatePosition);
    }

    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
      window.removeEventListener('scroll', updatePosition, { capture: true } as EventListenerOptions);
      window.removeEventListener('resize', updatePosition);
    };
  }, [isOpen, updatePosition, onClose, extraRef]);

  return {
    triggerRef,
    popoverRef,
    position,
    updatePosition,
  };
}
