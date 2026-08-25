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
