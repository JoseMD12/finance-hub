import { useQuery } from '@tanstack/react-query';
import { transactionsKeys } from '../api/transactionsKeys';
import { getTransactionsApi } from '../api/transactionsApi';
import type { PaginatedTransactionsDto, TransactionFilterParams } from '../types/transactions.types';
import type { ApiError } from '@/shared/types/api.types';

export function useTransactionsQuery(filters: TransactionFilterParams = {}) {
  return useQuery<PaginatedTransactionsDto, ApiError>({
    queryKey: transactionsKeys.list(filters),
    queryFn: ({ signal }) => getTransactionsApi(filters, signal),
    staleTime: 1000 * 60 * 2, // 2 minutos
  });
}
