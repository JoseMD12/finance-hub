import React, { useEffect, useRef } from 'react';
import { cn } from '@/shared/utils/cn';
import { X } from 'lucide-react';

export interface ModalProps {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  children: React.ReactNode;
  className?: string;
}

export const Modal: React.FC<ModalProps> = ({ isOpen, onClose, title, children, className }) => {
  const dialogRef = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    const dialog = dialogRef.current;
    if (!dialog) return;
    if (isOpen) {
      dialog.showModal();
    } else {
      dialog.close();
    }
  }, [isOpen]);

  useEffect(() => {
    const dialog = dialogRef.current;
    if (!dialog) return;
    const handleCancel = (e: Event) => {
      e.preventDefault();
      onClose();
    };
    dialog.addEventListener('cancel', handleCancel);
    return () => dialog.removeEventListener('cancel', handleCancel);
  }, [onClose]);

  return (
    <dialog
      ref={dialogRef}
      aria-labelledby="modal-title"
      className={cn(
        'w-full max-w-lg bg-white rounded-3xl p-6 shadow-elevated border border-border-subtle',
        'backdrop:bg-slate-900/40 backdrop:backdrop-blur-sm',
        'open:animate-in open:fade-in open:zoom-in-95 open:duration-200',
        className
      )}
    >
      <div className="flex items-center justify-between pb-4 mb-4 border-b border-slate-100">
        <h3 id="modal-title" className="text-lg font-bold text-secondary">
          {title}
        </h3>
        <button
          type="button"
          onClick={onClose}
          aria-label="Fechar diálogo"
          className="p-1 text-slate-400 hover:text-slate-600 rounded-lg hover:bg-slate-100 transition-colors"
        >
          <X className="w-5 h-5" />
        </button>
      </div>
      <div>{children}</div>
    </dialog>
  );
};
