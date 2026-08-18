/**
 * Query Key Factory for Connections Feature.
 * Rule 15: Server State is managed exclusively via TanStack Query v5 with strongly-typed Query Key Factories.
 */
export const connectionKeys = {
  all: ['connections'] as const,
  status: () => [...connectionKeys.all, 'status'] as const,
  accounts: () => [...connectionKeys.all, 'accounts'] as const,
};
