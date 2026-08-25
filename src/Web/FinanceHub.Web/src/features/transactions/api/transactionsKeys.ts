import type { TransactionFilterParams } from '../types/transactions.types';

export const transactionsKeys = {
  all: ['transactions'] as const,
  lists: () => [...transactionsKeys.all, 'list'] as const,
  list: (filters: TransactionFilterParams) => [...transactionsKeys.lists(), filters] as const,
  categories: () => [...transactionsKeys.all, 'categories'] as const,
};
