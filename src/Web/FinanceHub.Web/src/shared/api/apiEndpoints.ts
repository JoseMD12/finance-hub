/**
 * Centralized API Endpoints Catalog for FinanceHub Frontend.
 * Rule 23: All HTTP API endpoints MUST be strictly centralized in dedicated endpoint constants or feature API files.
 */
export const API_ENDPOINTS = {
  AUTH: {
    LOGIN: '/api/v1/auth/login',
    REFRESH: '/api/v1/auth/refresh',
    DEV_TOKEN: '/api/v1/gateway/auth/dev-token',
  },
  PLUGGY: {
    SYNC: '/api/v1/gateway/pluggy/sync',
  },
  DASHBOARD: {
    SUMMARY: '/api/v1/gateway/dashboard',
  },
  TRANSACTIONS: {
    LIST: '/api/v1/gateway/transactions',
    CATEGORIZE: (id: string) => `/api/v1/gateway/transactions/${id}/category`,
  },
} as const;

/**
 * Centralized HTTP Header Names for FinanceHub Frontend.
 * Rule 10: Zero magic strings — all header keys must be centralized constants.
 */
export const API_HEADERS = {
  PLUGGY_ACCESS_TOKEN: 'X-Pluggy-Access-Token',
  CORRELATION_ID: 'X-Correlation-Id',
} as const;
