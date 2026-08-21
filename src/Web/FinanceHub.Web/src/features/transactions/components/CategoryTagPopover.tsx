import React, { useState, useRef, useEffect } from 'react';
import { Search, Check, Tag, Loader2 } from 'lucide-react';
import { CategoryTag } from './CategoryTag';
import { useCategoriesQuery } from '../hooks/useCategoriesQuery';
import { useCategorizeTransactionMutation } from '../hooks/useCategorizeTransactionMutation';
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
  const popoverRef = useRef<HTMLDivElement>(null);

  const { data: categories = [], isLoading } = useCategoriesQuery();
  const categorizeMutation = useCategorizeTransactionMutation();

  // Fechar ao clicar fora
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (popoverRef.current && !popoverRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };

    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside);
    }
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, [isOpen]);

  // Aplanar categorias e subcategorias para pesquisa rápida
  const allFlattenedCategories: CategoryDto[] = React.useMemo(() => {
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

  const filteredCategories = allFlattenedCategories.filter((c) =>
    c.name.toLowerCase().includes(searchTerm.trim().toLowerCase())
  );

  const handleSelectCategory = async (categoryId: string) => {
    if (categoryId === currentCategoryId) {
      setIsOpen(false);
      return;
    }

    await categorizeMutation.mutateAsync({
      transactionId,
      categoryId,
      createCustomRule,
    });

    setIsOpen(false);
  };

  return (
    <div className="relative inline-block" ref={popoverRef}>
      <CategoryTag
        name={currentCategory?.name || 'Não categorizado'}
        iconKey={currentCategory?.iconKey || 'tag'}
        colorToken={currentCategory?.colorToken || 'gray'}
        onClick={() => setIsOpen(!isOpen)}
        interactive={true}
      />

      {isOpen && (
        <div className="absolute left-0 top-full mt-2 z-50 w-72 p-3 bg-surface-card rounded-xl shadow-elevated border border-border-subtle flex flex-col gap-3 animate-in fade-in zoom-in-95 duration-150">
          <div className="flex items-center justify-between border-b border-border-subtle pb-2">
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
            <Search className="w-3.5 h-3.5 absolute left-2.5 top-1/2 -translate-y-1/2 text-slate-400" />
            <input
              type="text"
              placeholder="Buscar categoria..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="w-full pl-8 pr-3 py-1.5 text-xs rounded-lg border border-border-subtle bg-surface-ground text-slate-800 focus:outline-none focus:border-brand transition-colors"
            />
          </div>

          {/* Lista de categorias */}
          <div className="max-h-48 overflow-y-auto flex flex-col gap-1 pr-1">
            {isLoading && (
              <div className="py-4 text-center text-xs text-slate-400">Carregando catálogo...</div>
            )}

            {!isLoading && filteredCategories.length === 0 && (
              <div className="py-4 text-center text-xs text-slate-400">Nenhuma categoria encontrada</div>
            )}

            {!isLoading &&
              filteredCategories.map((category) => {
                const isSelected = category.id === currentCategoryId;
                const isSub = !!category.parentCategoryId;

                return (
                  <button
                    key={category.id}
                    type="button"
                    onClick={() => handleSelectCategory(category.id)}
                    className={`flex items-center justify-between px-2.5 py-1.5 rounded-lg text-xs font-medium text-left transition-colors cursor-pointer ${
                      isSelected
                        ? 'bg-brand-light text-brand font-bold'
                        : 'hover:bg-slate-100 text-slate-700'
                    } ${isSub ? 'pl-5 text-slate-600' : ''}`}
                  >
                    <span className="truncate">{category.name}</span>
                    {isSelected && <Check className="w-3.5 h-3.5 text-brand shrink-0" />}
                  </button>
                );
              })}
          </div>

          {/* Opção de Regra Customizada */}
          <div className="pt-2 border-t border-border-subtle">
            <label className="flex items-center gap-2 text-[11px] text-slate-600 cursor-pointer select-none">
              <input
                type="checkbox"
                checked={createCustomRule}
                onChange={(e) => setCreateCustomRule(e.target.checked)}
                className="rounded border-slate-300 text-brand focus:ring-brand"
              />
              <span>Aplicar para transações similares futuras</span>
            </label>
          </div>
        </div>
      )}
    </div>
  );
};
