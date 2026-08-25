import React, { useState, useRef, useEffect, useCallback } from 'react';
import ReactDOM from 'react-dom';
import { MessageSquare, X, Check, Trash2, Loader2 } from 'lucide-react';
import { useUpdateTransactionNotesMutation } from '../hooks/useUpdateTransactionNotesMutation';
import { cn } from '@/shared/utils/cn';

export interface TransactionNotePopoverProps {
  transactionId: string;
  currentNotes?: string | null;
  description: string;
}

export const TransactionNotePopover: React.FC<TransactionNotePopoverProps> = ({
  transactionId,
  currentNotes,
  description,
}) => {
  const [isOpen, setIsOpen] = useState(false);
  const [notes, setNotes] = useState(currentNotes || '');
  const [coords, setCoords] = useState<{ top: number; left: number }>({ top: 0, left: 0 });

  const triggerRef = useRef<HTMLButtonElement>(null);
  const menuRef = useRef<HTMLDivElement>(null);

  const notesMutation = useUpdateTransactionNotesMutation();
  const hasNotes = Boolean(currentNotes && currentNotes.trim().length > 0);

  useEffect(() => {
    setNotes(currentNotes || '');
  }, [currentNotes]);

  const updatePosition = useCallback(() => {
    if (!triggerRef.current) return;
    const rect = triggerRef.current.getBoundingClientRect();
    const popoverWidth = 288; // w-72 (288px)
    const popoverHeight = 220;

    let left = rect.right - popoverWidth;
    if (left < 16) left = 16;

    let top = rect.bottom + 6;
    if (top + popoverHeight > window.innerHeight && rect.top > popoverHeight) {
      top = Math.max(16, rect.top - popoverHeight - 6);
    }

    setCoords({ top, left });
  }, []);

  const handleOpen = () => {
    updatePosition();
    setIsOpen((prev) => !prev);
  };

  const handleClose = () => {
    setIsOpen(false);
  };

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      const target = event.target as Node;
      const clickedTrigger = triggerRef.current?.contains(target);
      const clickedMenu = menuRef.current?.contains(target);

      if (!clickedTrigger && !clickedMenu) {
        setIsOpen(false);
      }
    };

    if (isOpen) {
      updatePosition();
      document.addEventListener('mousedown', handleClickOutside);
      window.addEventListener('scroll', updatePosition, true);
      window.addEventListener('resize', updatePosition);
    }

    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
      window.removeEventListener('scroll', updatePosition, true);
      window.removeEventListener('resize', updatePosition);
    };
  }, [isOpen, updatePosition]);

  const handleSave = () => {
    const trimmed = notes.trim();
    if (trimmed !== (currentNotes || '')) {
      notesMutation.mutate(
        { transactionId, notes: trimmed || null },
        { onSuccess: () => handleClose() }
      );
    } else {
      handleClose();
    }
  };

  const handleRemove = () => {
    notesMutation.mutate(
      { transactionId, notes: null },
      {
        onSuccess: () => {
          setNotes('');
          handleClose();
        },
      }
    );
  };

  return (
    <>
      <button
        ref={triggerRef}
        type="button"
        onClick={handleOpen}
        title={hasNotes ? `Nota: ${currentNotes}` : 'Adicionar observação'}
        aria-label={`Observações da transação ${description}`}
        className={cn(
          'w-8 h-8 p-2 rounded-xl border transition-all duration-150 cursor-pointer active:scale-95 flex items-center justify-center shrink-0',
          hasNotes
            ? 'bg-secondary-light text-secondary border-secondary/30 hover:bg-secondary/20 shadow-2xs'
            : 'bg-transparent text-slate-400 border-transparent hover:text-secondary hover:bg-secondary-light hover:border-secondary/20'
        )}
      >
        <MessageSquare className="w-4 h-4 shrink-0" />
      </button>

      {isOpen &&
        ReactDOM.createPortal(
          <div
            ref={menuRef}
            className="fixed z-50 animate-in fade-in zoom-in-95 duration-150"
            style={{ top: `${coords.top}px`, left: `${coords.left}px` }}
          >
            <div className="w-72 bg-surface-card rounded-2xl border border-border-subtle shadow-dropdown p-4 flex flex-col gap-3">
              {/* Header */}
              <div className="flex items-center justify-between pb-2 border-b border-border-subtle">
                <div className="flex items-center gap-1.5 font-bold text-xs text-slate-800">
                  <MessageSquare className="w-4 h-4 text-secondary" />
                  <span>Observação / Nota</span>
                </div>
                <button
                  type="button"
                  onClick={handleClose}
                  className="p-1 rounded-lg text-slate-400 hover:text-slate-700 hover:bg-slate-100 cursor-pointer transition-colors"
                >
                  <X className="w-3.5 h-3.5" />
                </button>
              </div>

              {/* Textarea Area */}
              <div className="flex flex-col gap-1.5">
                <textarea
                  value={notes}
                  onChange={(e) => setNotes(e.target.value)}
                  maxLength={500}
                  placeholder="Escreva uma anotação sobre esta transação..."
                  className="w-full h-24 p-3 rounded-lg border border-border-subtle bg-surface-ground text-xs text-slate-800 placeholder-slate-400 focus:outline-none focus:border-secondary focus:ring-2 focus:ring-secondary/20 transition-all resize-none leading-relaxed"
                  autoFocus
                />
                <div className="flex justify-end text-[10px] text-slate-400 font-medium px-0.5">
                  {notes.length}/500
                </div>
              </div>

              {/* Action Buttons Footer */}
              <div className="flex items-center justify-between pt-2 border-t border-border-subtle">
                {hasNotes ? (
                  <button
                    type="button"
                    onClick={handleRemove}
                    disabled={notesMutation.isPending}
                    className="px-2 py-1.5 rounded-lg text-rose-500 hover:bg-rose-50 hover:text-rose-600 text-xs font-semibold flex items-center gap-1 cursor-pointer transition-all active:scale-95"
                    title="Excluir nota"
                  >
                    <Trash2 className="w-3.5 h-3.5" />
                    <span>Excluir</span>
                  </button>
                ) : (
                  <div />
                )}

                <div className="flex items-center gap-2">
                  <button
                    type="button"
                    onClick={handleClose}
                    className="px-3 py-1.5 rounded-lg bg-slate-100 text-slate-600 text-xs font-semibold hover:bg-slate-200 active:scale-95 cursor-pointer transition-all"
                  >
                    Cancelar
                  </button>
                  <button
                    type="button"
                    onClick={handleSave}
                    disabled={notesMutation.isPending}
                    className="px-3 py-1.5 rounded-lg bg-secondary text-white text-xs font-semibold hover:bg-secondary-dark active:scale-95 cursor-pointer transition-all flex items-center gap-1 shadow-2xs"
                  >
                    {notesMutation.isPending ? (
                      <Loader2 className="w-3.5 h-3.5 animate-spin" />
                    ) : (
                      <Check className="w-3.5 h-3.5" />
                    )}
                    <span>Salvar</span>
                  </button>
                </div>
              </div>
            </div>
          </div>,
          document.body
        )}
    </>
  );
};
