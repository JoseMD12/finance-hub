import { httpClient } from '@/shared/api/httpClient';
import { API_ENDPOINTS } from '@/shared/api/apiEndpoints';
import type { PaginatedTransactionsDto } from '../types/transactions.types';

export const getTransactionsApi = async (
  page: number = 1,
  pageSize: number = 20,
  signal?: AbortSignal
): Promise<PaginatedTransactionsDto> => {
  const response = await httpClient.get<PaginatedTransactionsDto>(API_ENDPOINTS.TRANSACTIONS.LIST, {
    params: { page, pageSize },
    signal,
  });
  return response.data;
};
