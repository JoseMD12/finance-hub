import React, { useState, useRef, useEffect, useCallback, useMemo } from 'react';
import ReactDOM from 'react-dom';
import { Search, Tag, Loader2 } from 'lucide-react';
import { CategoryTag } from './CategoryTag';
import { CategoryCatalogList } from './CategoryCatalogList';
import { useCategoriesQuery } from '../hooks/useCategoriesQuery';
import { useCategorizeTransactionMutation } from '../hooks/useCategorizeTransactionMutation';
import { Checkbox } from '@/shared/components/Checkbox/Checkbox';
import type { CategoryDto } from '../types/transactions.types';

export interface CategoryTagPopoverProps {
  transactionId: string;
  currentCategoryId: string;
}

export const CategoryTagPopover: React.FC<CategoryTagPopoverProps> = ({
  transactionId,
  currentCategoryId,
}) => {
  const [isOpen, setIsOpen] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');
  const [createCustomRule, setCreateCustomRule] = useState(false);
  const [applyToPastTransactions, setApplyToPastTransactions] = useState(false);
  const [expandedParentIds, setExpandedParentIds] = useState<Set<string>>(new Set());
  const [position, setPosition] = useState<{ top: number; left: number } | null>(null);

  const popoverTriggerRef = useRef<HTMLDivElement>(null);
  const dropdownContentRef = useRef<HTMLDialogElement>(null);
  const searchInputRef = useRef<HTMLInputElement>(null);

  const { data: categories = [], isLoading } = useCategoriesQuery();
  const categorizeMutation = useCategorizeTransactionMutation();

  // Calcular posição flutuante precisa para o portal
  const updatePosition = useCallback(() => {
    if (!popoverTriggerRef.current) return;
    const rect = popoverTriggerRef.current.getBoundingClientRect();
    const dropdownHeight = 360;
    const dropdownWidth = 288; // w-72 (18rem = 288px)

    let top = rect.bottom + 6;
    // Se estourar a parte inferior da janela, abre para cima
    if (top + dropdownHeight > window.innerHeight && rect.top > dropdownHeight) {
      top = Math.max(10, rect.top - dropdownHeight - 6);
    }

    let left = rect.left;
    // Se estourar a borda direita da janela, ajusta para a esquerda
    if (left + dropdownWidth > window.innerWidth - 16) {
      left = Math.max(16, window.innerWidth - dropdownWidth - 16);
    }

    setPosition({ top, left });
  }, []);

  // Fechar ao clicar fora (verificando tanto o gatilho quanto o conteúdo do portal)
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      const target = event.target as Node;
      const clickedTrigger = popoverTriggerRef.current?.contains(target);
      const clickedDropdown = dropdownContentRef.current?.contains(target);

      if (!clickedTrigger && !clickedDropdown) {
        setIsOpen(false);
      }
    };

    if (isOpen) {
      updatePosition();
      document.addEventListener('mousedown', handleClickOutside);
      window.addEventListener('scroll', updatePosition, true);
      window.addEventListener('resize', updatePosition);

      // Foco automático no input de busca ao abrir
      setTimeout(() => {
        searchInputRef.current?.focus();
      }, 50);
    }

    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
      window.removeEventListener('scroll', updatePosition, true);
      window.removeEventListener('resize', updatePosition);
    };
  }, [isOpen, updatePosition]);

  // Aplanar categorias e subcategorias para pesquisa rápida
  const allFlattenedCategories: CategoryDto[] = useMemo(() => {
    const list: CategoryDto[] = [];
    categories.forEach((cat) => {
      list.push(cat);
      if (cat.subcategories) {
        cat.subcategories.forEach((sub) => list.push(sub));
      }
    });
    return list;
  }, [categories]);

  const currentCategory = allFlattenedCategories.find((c) => c.id === currentCategoryId);

  // Inicializar o pai da categoria atual expandido quando o popover abrir
  useEffect(() => {
    if (isOpen && currentCategory?.parentCategoryId) {
      setExpandedParentIds((prev) => new Set([...prev, currentCategory.parentCategoryId!]));
    }
  }, [isOpen, currentCategory]);

  const toggleExpand = (parentId: string, e: React.MouseEvent) => {
    e.stopPropagation();
    setExpandedParentIds((prev) => {
      const next = new Set(prev);
      if (next.has(parentId)) {
        next.delete(parentId);
      } else {
        next.add(parentId);
      }
      return next;
    });
  };

  const handleSelectCategory = async (categoryId: string) => {
    if (categoryId === currentCategoryId) {
      setIsOpen(false);
      return;
    }

    await categorizeMutation.mutateAsync({
      transactionId,
      categoryId,
      createCustomRule,
      applyToPastTransactions,
    });

    setIsOpen(false);
  };

  const isSearching = searchTerm.trim().length > 0;
  const filteredSearchCategories = allFlattenedCategories.filter((c) =>
    c.name.toLowerCase().includes(searchTerm.trim().toLowerCase())
  );

  return (
    <div className="relative inline-block" ref={popoverTriggerRef}>
      <CategoryTag
        name={currentCategory?.name || 'Não categorizado'}
        iconKey={currentCategory?.iconKey || 'tag'}
        colorToken={currentCategory?.colorToken || 'gray'}
        onClick={() => setIsOpen(!isOpen)}
        interactive={true}
      />

      {isOpen &&
        position &&
        ReactDOM.createPortal(
          <dialog
            open
            ref={dropdownContentRef}
            aria-label="Alterar categoria da transação"
            style={{
              position: 'fixed',
              top: `${position.top}px`,
              left: `${position.left}px`,
            }}
            className="z-[9999] w-72 p-3.5 bg-surface-card rounded-2xl shadow-elevated border border-border-subtle flex flex-col gap-3 animate-in fade-in zoom-in-95 duration-150 m-0"
          >
            <div className="flex items-center justify-between border-b border-border-subtle pb-2.5">
              <span className="text-xs font-bold text-secondary flex items-center gap-1.5">
                <Tag className="w-3.5 h-3.5 text-brand" />
                Alterar Categoria
              </span>
              {categorizeMutation.isPending && (
                <Loader2 className="w-3.5 h-3.5 text-brand animate-spin" />
              )}
            </div>

            {/* Busca de categorias */}
            <div className="relative">
              <Search className="w-3.5 h-3.5 absolute left-2.5 top-1/2 -translate-y-1/2 text-slate-400 pointer-events-none" />
              <input
                ref={searchInputRef}
                type="text"
                placeholder="Buscar categoria ou subcategoria..."
                value={searchTerm}
                aria-label="Filtrar categorias"
                onChange={(e) => setSearchTerm(e.target.value)}
                className="w-full pl-8 pr-3 py-1.5 text-xs rounded-lg border border-border-subtle bg-surface-ground text-slate-800 focus:outline-none focus:border-brand focus:ring-2 focus:ring-brand/20 transition-all"
              />
            </div>

            {/* Lista de categorias Reutilizável */}
            <div className="max-h-56 overflow-y-auto flex flex-col gap-1 pr-1">
              <CategoryCatalogList
                isLoading={isLoading}
                isSearching={isSearching}
                categories={categories}
                filteredSearchCategories={filteredSearchCategories}
                selectedCategoryId={currentCategoryId}
                expandedParentIds={expandedParentIds}
                onSelectCategory={(id) => handleSelectCategory(id)}
                onToggleExpand={toggleExpand}
              />
            </div>

            {/* Opções de Automação de Categoria */}
            <div className="pt-2.5 border-t border-border-subtle flex flex-col gap-2">
              <Checkbox
                checked={createCustomRule}
                onChange={setCreateCustomRule}
                label={<span className="text-[11px] font-medium text-slate-600">Criar regra para transações futuras similares</span>}
              />

              <Checkbox
                checked={applyToPastTransactions}
                onChange={setApplyToPastTransactions}
                label={<span className="text-[11px] font-medium text-slate-600">Aplicar alteração em lançamentos passados similares</span>}
              />
            </div>
          </dialog>,
          document.body
        )}
    </div>
  );
};
