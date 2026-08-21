import React from 'react';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { CustomSelect } from '@/shared/components/Select/CustomSelect';

export interface TransactionsPaginationProps {
  currentPage: number;
  totalPages: number;
  pageSize: number;
  totalItems: number;
  onPageChange: (page: number) => void;
  onPageSizeChange: (pageSize: number) => void;
}

export const TransactionsPagination: React.FC<TransactionsPaginationProps> = ({
  currentPage,
  totalPages,
  pageSize,
  totalItems,
  onPageChange,
  onPageSizeChange,
}) => {
  const pageSizeOptions = [
    { value: '10', label: '10 itens' },
    { value: '20', label: '20 itens' },
    { value: '50', label: '50 itens' },
    { value: '100', label: '100 itens' },
  ];

  const startItem = totalItems === 0 ? 0 : (currentPage - 1) * pageSize + 1;
  const endItem = Math.min(currentPage * pageSize, totalItems);

  // Gerar botões de página numerados
  const getPageNumbers = () => {
    const pages: { id: string; label: number | string; pageNumber?: number }[] = [];
    const maxVisible = 5;

    if (totalPages <= maxVisible) {
      for (let i = 1; i <= totalPages; i++) {
        pages.push({ id: `page-${i}`, label: i, pageNumber: i });
      }
    } else if (currentPage <= 3) {
      [1, 2, 3, 4].forEach((p) => pages.push({ id: `page-${p}`, label: p, pageNumber: p }));
      pages.push({ id: 'ellipsis-end', label: '...' });
      pages.push({ id: `page-${totalPages}`, label: totalPages, pageNumber: totalPages });
    } else if (currentPage >= totalPages - 2) {
      pages.push({ id: 'page-1', label: 1, pageNumber: 1 });
      pages.push({ id: 'ellipsis-start', label: '...' });
      for (let i = totalPages - 3; i <= totalPages; i++) {
        pages.push({ id: `page-${i}`, label: i, pageNumber: i });
      }
    } else {
      pages.push({ id: 'page-1', label: 1, pageNumber: 1 });
      pages.push({ id: 'ellipsis-start', label: '...' });
      [currentPage - 1, currentPage, currentPage + 1].forEach((p) =>
        pages.push({ id: `page-${p}`, label: p, pageNumber: p })
      );
      pages.push({ id: 'ellipsis-end', label: '...' });
      pages.push({ id: `page-${totalPages}`, label: totalPages, pageNumber: totalPages });
    }
    return pages;
  };

  if (totalItems === 0) return null;

  return (
    <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 p-4 bg-surface-card rounded-2xl border border-border-subtle text-xs text-slate-600">
      {/* Contador de Itens */}
      <div className="flex items-center gap-2">
        <span>
          Exibindo <strong className="text-secondary">{startItem}–{endItem}</strong> de{' '}
          <strong className="text-secondary">{totalItems}</strong> transações
        </span>
      </div>

      {/* Navegação de Páginas e Seletor de PageSize */}
      <div className="flex items-center gap-4">
        <div className="w-32">
          <CustomSelect
            options={pageSizeOptions}
            value={String(pageSize)}
            onChange={(val) => onPageSizeChange(Number(val))}
            label="Itens por página"
          />
        </div>

        <div className="flex items-center gap-1">
          {/* Botão Anterior */}
          <button
            type="button"
            onClick={() => onPageChange(currentPage - 1)}
            disabled={currentPage <= 1}
            aria-label="Página anterior"
            className="p-1.5 rounded-lg border border-border-subtle bg-surface-ground text-slate-600 hover:bg-slate-100 disabled:opacity-40 disabled:pointer-events-none transition-colors cursor-pointer"
          >
            <ChevronLeft className="w-4 h-4" />
          </button>

          {/* Páginas numeradas */}
          {getPageNumbers().map((item) =>
            item.pageNumber !== undefined ? (
              <button
                key={item.id}
                type="button"
                onClick={() => onPageChange(item.pageNumber!)}
                aria-label={`Página ${item.label}`}
                aria-current={item.pageNumber === currentPage ? 'page' : undefined}
                className={`min-w-8 h-8 px-2 rounded-lg font-bold transition-colors cursor-pointer ${
                  item.pageNumber === currentPage
                    ? 'bg-brand text-white shadow-sm'
                    : 'bg-surface-ground text-slate-600 hover:bg-slate-100 border border-border-subtle'
                }`}
              >
                {item.label}
              </button>
            ) : (
              <span key={item.id} className="px-1 text-slate-400">
                {item.label}
              </span>
            )
          )}

          {/* Botão Próximo */}
          <button
            type="button"
            onClick={() => onPageChange(currentPage + 1)}
            disabled={currentPage >= totalPages}
            aria-label="Próxima página"
            className="p-1.5 rounded-lg border border-border-subtle bg-surface-ground text-slate-600 hover:bg-slate-100 disabled:opacity-40 disabled:pointer-events-none transition-colors cursor-pointer"
          >
            <ChevronRight className="w-4 h-4" />
          </button>
        </div>
      </div>
    </div>
  );
};
