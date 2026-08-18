export const connectionKeys = {
  all: ['connections'] as const,
  status: () => [...connectionKeys.all, 'status'] as const,
  items: (token: string) => [...connectionKeys.all, 'items', token] as const,
};
