import React from 'react';
import { Check, ChevronDown, ChevronRight } from 'lucide-react';
import { cn } from '@/shared/utils/cn';
import { getCategoryIcon } from '../utils/categoryIcons';
import type { CategoryDto } from '../types/transactions.types';

export interface CategoryCatalogListProps {
  isLoading: boolean;
  isSearching: boolean;
  categories: CategoryDto[];
  filteredSearchCategories: CategoryDto[];
  selectedCategoryId?: string | null;
  expandedParentIds: Set<string>;
  onSelectCategory: (categoryId: string) => void;
  onToggleExpand: (parentId: string, e: React.MouseEvent) => void;
}

export const CategoryCatalogList: React.FC<CategoryCatalogListProps> = ({
  isLoading,
  isSearching,
  categories,
  filteredSearchCategories,
  selectedCategoryId,
  expandedParentIds,
  onSelectCategory,
  onToggleExpand,
}) => {
  if (isLoading) {
    return <div className="py-6 text-center text-xs text-slate-400">Carregando catálogo...</div>;
  }

  if (isSearching && filteredSearchCategories.length === 0) {
    return <div className="py-6 text-center text-xs text-slate-400">Nenhuma categoria encontrada</div>;
  }

  if (isSearching) {
    return (
      <>
        {filteredSearchCategories.map((category) => {
          const isSelected = category.id === selectedCategoryId;
          const isSub = !!category.parentCategoryId;
          const ItemIcon = getCategoryIcon(category.iconKey);

          return (
            <button
              key={category.id}
              type="button"
              onClick={() => onSelectCategory(category.id)}
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
      </>
    );
  }

  return (
    <>
      {categories.map((parent) => {
        const hasSub = !!(parent.subcategories && parent.subcategories.length > 0);
        const isExpanded = expandedParentIds.has(parent.id);
        const isParentSelected = parent.id === selectedCategoryId;
        const ParentIcon = getCategoryIcon(parent.iconKey);

        return (
          <div key={parent.id} className="flex flex-col gap-0.5">
            <div className="flex items-center justify-between rounded-lg">
              <button
                type="button"
                onClick={() => onSelectCategory(parent.id)}
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
                  onClick={(e) => onToggleExpand(parent.id, e)}
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

            {hasSub && isExpanded && (
              <div className="flex flex-col gap-0.5 pl-3 border-l-2 border-slate-200 ml-3.5 my-0.5">
                {parent.subcategories!.map((sub) => {
                  const isSubSelected = sub.id === selectedCategoryId;
                  const SubIcon = getCategoryIcon(sub.iconKey);

                  return (
                    <button
                      key={sub.id}
                      type="button"
                      onClick={() => onSelectCategory(sub.id)}
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
    </>
  );
};
