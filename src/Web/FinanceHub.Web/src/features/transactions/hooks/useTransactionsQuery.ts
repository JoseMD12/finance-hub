import { useQuery } from '@tanstack/react-query';
import { transactionKeys } from '../api/transactionKeys';
import { getTransactionsApi } from '../api/transactionsApi';
import type { PaginatedTransactionsDto } from '../types/transactions.types';
import type { ApiError } from '@/shared/types/api.types';

export function useTransactionsQuery(page: number = 1, bankFilter?: string) {
  return useQuery<PaginatedTransactionsDto, ApiError>({
    queryKey: transactionKeys.list(bankFilter, page),
    queryFn: ({ signal }) => getTransactionsApi(page, 20, signal),
    staleTime: 1000 * 60 * 2, // 2 minutos
  });
}
