import React, { useState, useEffect, useCallback } from 'react';
import ReactDOM from 'react-dom';
import { MessageSquare, X, Check, Trash2, Loader2 } from 'lucide-react';
import { useUpdateTransactionNotesMutation } from '../hooks/useUpdateTransactionNotesMutation';
import { cn } from '@/shared/utils/cn';
import { useFloatingPopover } from '@/shared/hooks/useFloatingPopover';

export interface TransactionNotePopoverProps {
  transactionId: string;
  currentNotes?: string | null;
  description: string;
}

const TransactionNotePopoverComponent: React.FC<TransactionNotePopoverProps> = ({
  transactionId,
  currentNotes,
  description,
}) => {
  const [isOpen, setIsOpen] = useState(false);
  const [notes, setNotes] = useState(currentNotes || '');

  const notesMutation = useUpdateTransactionNotesMutation();
  const hasNotes = Boolean(currentNotes && currentNotes.trim().length > 0);

  useEffect(() => {
    setNotes(currentNotes || '');
  }, [currentNotes]);

  const handleClose = useCallback(() => {
    setIsOpen(false);
  }, []);

  const { triggerRef, popoverRef, position } = useFloatingPopover({
    isOpen,
    onClose: handleClose,
    width: 288,
    height: 220,
    align: 'right',
  });

  const handleOpen = () => {
    setIsOpen((prev) => !prev);
  };

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
    <div className="relative inline-flex items-center justify-center">
      <button
        ref={triggerRef as React.RefObject<HTMLButtonElement>}
        type="button"
        onClick={handleOpen}
        title={hasNotes ? `Nota: ${currentNotes}` : 'Adicionar observação'}
        aria-label={`Observações da transação ${description}`}
        className={cn(
          'p-1 rounded-lg transition-colors cursor-pointer flex items-center justify-center',
          hasNotes
            ? 'text-brand bg-brand-light/80 hover:bg-brand-light'
            : 'text-slate-400 hover:text-slate-600 hover:bg-slate-100'
        )}
      >
        <MessageSquare className={cn('w-3.5 h-3.5', hasNotes && 'fill-brand/20')} />
      </button>

      {isOpen &&
        position &&
        ReactDOM.createPortal(
          <div
            ref={popoverRef as React.RefObject<HTMLDivElement>}
            style={{
              position: 'fixed',
              top: `${position.top}px`,
              left: `${position.left}px`,
            }}
            className="z-[9999] w-72 p-3 bg-surface-card rounded-2xl shadow-elevated border border-border-subtle flex flex-col gap-2.5 select-none text-slate-800"
          >
            {/* Header */}
            <div className="flex items-center justify-between pb-2 border-b border-border-subtle/80">
              <div className="flex items-center gap-1.5 min-w-0 pr-2">
                <MessageSquare className="w-3.5 h-3.5 text-brand shrink-0" />
                <span className="text-xs font-bold text-slate-800 truncate">Observação / Nota</span>
              </div>
              <button
                type="button"
                onClick={handleClose}
                className="p-1 rounded-lg text-slate-400 hover:text-slate-600 hover:bg-slate-100 transition-colors cursor-pointer shrink-0"
                aria-label="Fechar"
              >
                <X className="w-3.5 h-3.5" />
              </button>
            </div>

            <p className="text-[11px] text-slate-500 truncate -mt-1">{description}</p>

            {/* Input Área de Texto */}
            <div className="flex flex-col gap-1">
              <textarea
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
                placeholder="Escreva uma anotação sobre esta transação..."
                maxLength={500}
                rows={3}
                className="w-full p-2 text-xs rounded-xl border border-border-subtle bg-surface-ground text-slate-800 placeholder:text-slate-400 focus:outline-none focus:border-brand focus:ring-1 focus:ring-brand/20 transition-colors resize-none"
                autoFocus
              />
              <div className="flex justify-end">
                <span className="text-[10px] text-slate-400 font-mono">
                  {notes.length}/500
                </span>
              </div>
            </div>

            {/* Footer com Ações */}
            <div className="flex items-center justify-between pt-2 border-t border-border-subtle/80 text-xs">
              {hasNotes ? (
                <button
                  type="button"
                  onClick={handleRemove}
                  disabled={notesMutation.isPending}
                  className="px-2 py-1 rounded-lg text-status-danger hover:bg-status-danger-bg transition-colors cursor-pointer inline-flex items-center gap-1 text-[11px] font-semibold disabled:opacity-50"
                  title="Excluir nota"
                >
                  <Trash2 className="w-3 h-3" />
                  Excluir
                </button>
              ) : (
                <div />
              )}

              <div className="flex items-center gap-1.5">
                <button
                  type="button"
                  onClick={handleClose}
                  disabled={notesMutation.isPending}
                  className="px-2.5 py-1 rounded-lg text-slate-600 hover:bg-slate-100 hover:text-slate-800 transition-colors cursor-pointer font-medium text-[11px] disabled:opacity-50"
                >
                  Cancelar
                </button>
                <button
                  type="button"
                  onClick={handleSave}
                  disabled={notesMutation.isPending}
                  className="px-3 py-1 rounded-lg font-bold bg-brand text-white hover:bg-brand-dark active:scale-[0.98] transition-colors cursor-pointer shadow-2xs inline-flex items-center gap-1 text-[11px] disabled:opacity-50"
                >
                  {notesMutation.isPending ? (
                    <Loader2 className="w-3 h-3 animate-spin" />
                  ) : (
                    <Check className="w-3 h-3" />
                  )}
                  Salvar
                </button>
              </div>
            </div>
          </div>,
          document.body
        )}
    </div>
  );
};

export const TransactionNotePopover = React.memo(TransactionNotePopoverComponent);
