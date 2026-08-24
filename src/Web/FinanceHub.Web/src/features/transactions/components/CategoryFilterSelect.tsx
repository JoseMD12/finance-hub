import React, { useState, useRef, useEffect, useMemo } from 'react';
import { Search, Check, Tag, ChevronDown, ChevronRight, X } from 'lucide-react';
import { cn } from '@/shared/utils/cn';
import { useCategoriesQuery } from '../hooks/useCategoriesQuery';
import { getCategoryIcon } from '../utils/categoryIcons';
import type { CategoryDto } from '../types/transactions.types';

export interface CategoryFilterSelectProps {
  value?: string;
  onChange: (categoryId: string | undefined) => void;
  label?: string;
  className?: string;
}

export const CategoryFilterSelect: React.FC<CategoryFilterSelectProps> = ({
  value = '',
  onChange,
  label = 'Categoria',
  className,
}) => {
  const [isOpen, setIsOpen] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');
  const [expandedParentIds, setExpandedParentIds] = useState<Set<string>>(new Set());
  const containerRef = useRef<HTMLDivElement>(null);
  const searchInputRef = useRef<HTMLInputElement>(null);

  const { data: categories = [], isLoading } = useCategoriesQuery();

  // Fechar ao clicar fora
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };

    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside);
      // Auto-foco no campo de busca ao abrir
      setTimeout(() => {
        searchInputRef.current?.focus();
      }, 50);
    }
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, [isOpen]);

  // Aplanar todas as categorias para busca rápida e lookup do label atual
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

  const selectedCategory = allFlattenedCategories.find((c) => c.id === value);

  // Expandir automaticamente o pai da categoria selecionada ao abrir
  useEffect(() => {
    if (isOpen && selectedCategory?.parentCategoryId) {
      setExpandedParentIds((prev) => new Set([...prev, selectedCategory.parentCategoryId!]));
    }
  }, [isOpen, selectedCategory]);

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

  const handleSelect = (categoryId: string | undefined) => {
    onChange(categoryId);
    setIsOpen(false);
    setSearchTerm('');
  };

  const isSearching = searchTerm.trim().length > 0;
  const filteredSearchCategories = allFlattenedCategories.filter((c) =>
    c.name.toLowerCase().includes(searchTerm.trim().toLowerCase())
  );

  return (
    <div className={cn('flex flex-col gap-1.5 w-full relative', className)} ref={containerRef}>
      {label && <label className="text-xs font-semibold text-slate-700 pl-1">{label}</label>}

      {/* Botão Gatilho do Select */}
      <div className={cn('relative w-full', isOpen && 'z-50')}>
        <button
          type="button"
          onClick={() => setIsOpen((prev) => !prev)}
          aria-haspopup="dialog"
          aria-expanded={isOpen}
          aria-label={label}
          className={cn(
            'flex items-center justify-between w-full h-10 px-4 py-2 text-xs font-medium bg-surface-ground border border-border-subtle rounded-xl cursor-pointer transition-all duration-200 outline-none select-none form-input-focus',
            isOpen
              ? 'border-brand bg-surface-card ring-2 ring-brand/20 shadow-sm'
              : 'hover:border-slate-300'
          )}
        >
          <span className="flex items-center gap-2 truncate">
            <Tag className="w-3.5 h-3.5 text-brand shrink-0" />
            <span
              className={cn(
                'truncate',
                selectedCategory ? 'text-slate-800 font-bold' : 'text-slate-600 font-medium'
              )}
            >
              {selectedCategory ? selectedCategory.name : 'Todas as Categorias'}
            </span>
          </span>

          <div className="flex items-center gap-1.5">
            {selectedCategory && (
              <button
                type="button"
                onClick={(e) => {
                  e.stopPropagation();
                  handleSelect(undefined);
                }}
                aria-label="Limpar categoria selecionada"
                className="p-1 rounded-md text-slate-400 hover:text-slate-600 hover:bg-slate-200/60 transition-colors cursor-pointer"
              >
                <X className="w-3.5 h-3.5" />
              </button>
            )}
            <ChevronDown
              className={cn(
                'w-4 h-4 text-slate-400 transition-transform duration-200',
                isOpen && 'rotate-180 text-brand'
              )}
            />
          </div>
        </button>

        {/* Dropdown Hierárquico e com Busca idêntico ao CategoryTagPopover */}
        {isOpen && (
          <div
            role="dialog"
            aria-label="Filtrar por Categoria"
            className="absolute top-[calc(100%+6px)] left-0 right-0 z-50 p-3 bg-surface-card border border-border-subtle rounded-2xl shadow-elevated flex flex-col gap-2.5 min-w-[280px] animate-in fade-in zoom-in-95 duration-150"
          >
            {/* Campo de Busca */}
            <div className="relative">
              <Search className="w-3.5 h-3.5 absolute left-2.5 top-1/2 -translate-y-1/2 text-slate-400 pointer-events-none" />
              <input
                ref={searchInputRef}
                type="text"
                placeholder="Buscar categoria ou subcategoria..."
                value={searchTerm}
                aria-label="Buscar categoria"
                onChange={(e) => setSearchTerm(e.target.value)}
                className="w-full pl-8 pr-3 py-1.5 text-xs rounded-lg border border-border-subtle bg-surface-ground text-slate-800 focus:outline-none focus:border-brand focus:ring-2 focus:ring-brand/20 transition-all"
              />
            </div>

            {/* Lista de Categorias */}
            <div className="max-h-60 overflow-y-auto flex flex-col gap-1 pr-1">
              {isLoading && (
                <div className="py-6 text-center text-xs text-slate-400">Carregando catálogo...</div>
              )}

              {/* Opção Padrão: Todas as Categorias (sempre visível quando não está buscando) */}
              {!isLoading && !isSearching && (
                <button
                  type="button"
                  onClick={() => handleSelect(undefined)}
                  className={cn(
                    'flex items-center justify-between px-2.5 py-2 rounded-lg text-xs font-semibold text-left transition-all cursor-pointer border border-transparent',
                    !value
                      ? 'bg-brand-light text-brand-dark font-bold border-brand/20 shadow-2xs'
                      : 'hover:bg-slate-100/80 text-slate-700 hover:text-slate-900'
                  )}
                >
                  <span className="truncate flex items-center gap-1.5">
                    <Tag className="w-3.5 h-3.5 text-brand" />
                    Todas as Categorias
                  </span>
                  {!value && <Check className="w-3.5 h-3.5 text-brand shrink-0" />}
                </button>
              )}

              {!isLoading && isSearching && filteredSearchCategories.length === 0 && (
                <div className="py-6 text-center text-xs text-slate-400">Nenhuma categoria encontrada</div>
              )}

              {/* Modo de Busca: exibe lista filtrada direta */}
              {!isLoading &&
                isSearching &&
                filteredSearchCategories.map((category) => {
                  const isSelected = category.id === value;
                  const isSub = !!category.parentCategoryId;
                  const ItemIcon = getCategoryIcon(category.iconKey);

                  return (
                    <button
                      key={category.id}
                      type="button"
                      onClick={() => handleSelect(category.id)}
                      className={cn(
                        'flex items-center justify-between px-2.5 py-2 rounded-lg text-xs font-medium text-left transition-all cursor-pointer',
                        isSelected
                          ? 'bg-brand-light text-brand-dark font-bold shadow-2xs'
                          : 'hover:bg-slate-100/80 text-slate-700 hover:text-slate-900',
                        isSub && 'pl-5 text-slate-600'
                      )}
                    >
                      <span className="truncate flex items-center gap-1.5">
                        {isSub && <span className="text-slate-300 select-none">└</span>}
                        <ItemIcon className="w-3.5 h-3.5 text-slate-500 shrink-0" aria-hidden="true" />
                        <span>{category.name}</span>
                      </span>
                      {isSelected && <Check className="w-3.5 h-3.5 text-brand shrink-0" />}
                    </button>
                  );
                })}

              {/* Modo Padrão: Categorias principais com subcategorias colapsáveis */}
              {!isLoading &&
                !isSearching &&
                categories.map((parent) => {
                  const hasSub = !!(parent.subcategories && parent.subcategories.length > 0);
                  const isExpanded = expandedParentIds.has(parent.id);
                  const isParentSelected = parent.id === value;
                  const ParentIcon = getCategoryIcon(parent.iconKey);

                  return (
                    <div key={parent.id} className="flex flex-col gap-0.5">
                      {/* Item Principal */}
                      <div className="flex items-center justify-between rounded-lg">
                        <button
                          type="button"
                          onClick={() => handleSelect(parent.id)}
                          className={cn(
                            'flex-1 flex items-center justify-between px-2.5 py-2 rounded-lg text-xs font-medium text-left transition-all cursor-pointer',
                            isParentSelected
                              ? 'bg-brand-light text-brand-dark font-bold shadow-2xs'
                              : 'hover:bg-slate-100/80 text-slate-700 hover:text-slate-900'
                          )}
                        >
                          <span className="truncate font-semibold flex items-center gap-2">
                            <ParentIcon className="w-3.5 h-3.5 text-slate-500 shrink-0" aria-hidden="true" />
                            <span>{parent.name}</span>
                          </span>
                          {isParentSelected && <Check className="w-3.5 h-3.5 text-brand shrink-0" />}
                        </button>

                        {hasSub && (
                          <button
                            type="button"
                            onClick={(e) => toggleExpand(parent.id, e)}
                            aria-label={
                              isExpanded
                                ? `Recolher subcategorias de ${parent.name}`
                                : `Expandir subcategorias de ${parent.name}`
                            }
                            title={isExpanded ? 'Recolher subcategorias' : 'Ver subcategorias'}
                            className="w-8 h-8 flex items-center justify-center rounded-lg text-slate-400 hover:text-brand hover:bg-brand-light active:scale-95 transition-all cursor-pointer shrink-0 ml-1"
                          >
                            {isExpanded ? (
                              <ChevronDown className="w-4 h-4" />
                            ) : (
                              <ChevronRight className="w-4 h-4" />
                            )}
                          </button>
                        )}
                      </div>

                      {/* Subcategorias expandidas */}
                      {hasSub && isExpanded && (
                        <div className="flex flex-col gap-0.5 pl-3 border-l-2 border-slate-200 ml-3.5 my-0.5 animate-in fade-in slide-in-from-top-1 duration-150">
                          {parent.subcategories!.map((sub) => {
                            const isSubSelected = sub.id === value;
                            const SubIcon = getCategoryIcon(sub.iconKey);

                            return (
                              <button
                                key={sub.id}
                                type="button"
                                onClick={() => handleSelect(sub.id)}
                                className={cn(
                                  'flex items-center justify-between px-2.5 py-1.5 rounded-lg text-xs font-medium text-left transition-all cursor-pointer',
                                  isSubSelected
                                    ? 'bg-brand-light text-brand-dark font-bold'
                                    : 'hover:bg-slate-100/80 text-slate-600 hover:text-slate-900'
                                )}
                              >
                                <span className="truncate flex items-center gap-1.5">
                                  <span className="text-slate-300 select-none">└</span>
                                  <SubIcon className="w-3.5 h-3.5 text-slate-400 shrink-0" aria-hidden="true" />
                                  <span>{sub.name}</span>
                                </span>
                                {isSubSelected && <Check className="w-3.5 h-3.5 text-brand shrink-0" />}
                              </button>
                            );
                          })}
                        </div>
                      )}
                    </div>
                  );
                })}
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
