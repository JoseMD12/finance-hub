export const RUNTIME_CONSTANTS = {
  pluggyHost: 'meu.pluggy.ai',
  pluggyAccessTokenHeader: 'x-pluggy-access-token',
  pluggyAccountsPath: '/api/v1/pluggy/accounts',
  financeHubTokenStorageKey: 'pluggy_access_token',
  logoutLockStorageKey: 'financehub.logout-lock-until',
  logoutLockDurationMs: 5_000,
  sidePanelCloseDelayMs: 2_500,
  requestTimeoutMs: 10_000,
  loginPathMarkers: ['/login', '/signin', '/sign-in'],
  logoutTextPattern: /\b(sair|logout|log\s*out|encerrar\s+sess[aã]o)\b/i,
} as const;

export const RUNTIME_URLS = {
  financeHubWeb: new URL('/conexoes', __FINANCEHUB_WEB_URL__).toString(),
  financeHubApi: __FINANCEHUB_API_URL__,
  pluggyAccounts: `${__FINANCEHUB_API_URL__}${RUNTIME_CONSTANTS.pluggyAccountsPath}`,
} as const;
