export const transactionKeys = {
  all: ['transactions'] as const,
  lists: () => [...transactionKeys.all, 'list'] as const,
  list: (bankFilter?: string, page: number = 1) => [...transactionKeys.lists(), { bankFilter, page }] as const,
};
