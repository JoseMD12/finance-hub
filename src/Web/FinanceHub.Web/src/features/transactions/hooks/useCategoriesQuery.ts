import { useQuery } from '@tanstack/react-query';
import { transactionsKeys } from '../api/transactionsKeys';
import { getCategoriesApi } from '../api/transactionsApi';
import type { CategoryDto } from '../types/transactions.types';
import type { ApiError } from '@/shared/types/api.types';

export function useCategoriesQuery() {
  return useQuery<CategoryDto[], ApiError>({
    queryKey: transactionsKeys.categories(),
    queryFn: ({ signal }) => getCategoriesApi(signal),
    staleTime: 1000 * 60 * 10, // 10 minutos
  });
}

export interface FlattenedCategoriesResult {
  categories: CategoryDto[];
  flatCategories: CategoryDto[];
  categoryMap: Map<string, CategoryDto>;
}

function selectFlattenedCategories(categories: CategoryDto[]): FlattenedCategoriesResult {
  const flatCategories: CategoryDto[] = [];
  const categoryMap = new Map<string, CategoryDto>();

  categories.forEach((cat) => {
    flatCategories.push(cat);
    categoryMap.set(cat.id, cat);
    if (cat.subcategories) {
      cat.subcategories.forEach((sub) => {
        flatCategories.push(sub);
        categoryMap.set(sub.id, sub);
      });
    }
  });

  return { categories, flatCategories, categoryMap };
}

export function useFlattenedCategoriesQuery() {
  return useQuery<CategoryDto[], ApiError, FlattenedCategoriesResult>({
    queryKey: transactionsKeys.categories(),
    queryFn: ({ signal }) => getCategoriesApi(signal),
    staleTime: 1000 * 60 * 10, // 10 minutos
    select: selectFlattenedCategories,
  });
}

