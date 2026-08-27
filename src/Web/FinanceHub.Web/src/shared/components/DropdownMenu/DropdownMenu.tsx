import React, { useState, useCallback } from 'react';
import ReactDOM from 'react-dom';
import { cn } from '@/shared/utils/cn';
import { useFloatingPopover } from '@/shared/hooks/useFloatingPopover';

export interface DropdownMenuItem {
  key: string;
  label: React.ReactNode;
  icon?: React.ReactNode;
  onClick: () => void;
  disabled?: boolean;
  variant?: 'default' | 'danger' | 'brand';
  checked?: boolean;
}

export interface DropdownMenuProps {
  trigger: React.ReactNode;
  items: DropdownMenuItem[];
  align?: 'left' | 'right';
  className?: string;
}

function getItemVariantClass(variant?: 'default' | 'danger' | 'brand'): string {
  if (variant === 'danger') return 'text-status-danger hover:bg-status-danger-bg';
  if (variant === 'brand') return 'text-brand hover:bg-brand-light';
  return 'text-slate-700 hover:bg-surface-ground hover:text-slate-900';
}

export const DropdownMenu: React.FC<DropdownMenuProps> = ({
  trigger,
  items,
  align = 'right',
  className,
}) => {
  const [isOpen, setIsOpen] = useState(false);
  const handleClose = useCallback(() => setIsOpen(false), []);

  const dropdownHeight = items.length * 40 + 16;
  const dropdownWidth = 200;

  const { triggerRef, popoverRef, position } = useFloatingPopover({
    isOpen,
    onClose: handleClose,
    width: dropdownWidth,
    height: dropdownHeight,
    align,
  });

  return (
    <div
      className={cn('relative inline-block', className)}
      ref={triggerRef as React.RefObject<HTMLDivElement>}
    >
      <button
        type="button"
        onClick={() => setIsOpen((prev) => !prev)}
        className="cursor-pointer bg-transparent border-0 p-0 text-left focus:outline-none"
      >
        {trigger}
      </button>

      {isOpen &&
        position &&
        ReactDOM.createPortal(
          <div
            ref={popoverRef as React.RefObject<HTMLDivElement>}
            role="menu"
            aria-orientation="vertical"
            style={{
              position: 'fixed',
              top: `${position.top}px`,
              left: `${position.left}px`,
            }}
            className="z-[9999] w-52 p-1.5 bg-surface-card rounded-2xl shadow-elevated border border-border-subtle flex flex-col gap-0.5 select-none"
          >
            {items.map((item) => (
              <button
                key={item.key}
                type="button"
                role="menuitem"
                disabled={item.disabled}
                onClick={() => {
                  if (!item.disabled) {
                    item.onClick();
                    setIsOpen(false);
                  }
                }}
                className={cn(
                  'flex items-center gap-2.5 px-3 py-2 rounded-xl text-xs font-semibold text-left transition-colors cursor-pointer w-full',
                  getItemVariantClass(item.variant),
                  item.checked && 'bg-brand-light text-brand-dark font-bold',
                  item.disabled && 'opacity-50 cursor-not-allowed'
                )}
              >
                {item.icon && <span className="w-4 h-4 shrink-0 flex items-center justify-center">{item.icon}</span>}
                <span className="truncate flex-1">{item.label}</span>
              </button>
            ))}
          </div>,
          document.body
        )}
    </div>
  );
};
