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

const webBaseUrl = typeof __FINANCEHUB_WEB_URL__ !== 'undefined' && __FINANCEHUB_WEB_URL__
  ? __FINANCEHUB_WEB_URL__
  : 'http://localhost:3000';
const apiBaseUrl = typeof __FINANCEHUB_API_URL__ !== 'undefined' && __FINANCEHUB_API_URL__
  ? __FINANCEHUB_API_URL__
  : 'http://localhost:5050';

export const RUNTIME_URLS = {
  meuPluggy: 'https://meu.pluggy.ai/overview',
  financeHubWeb: new URL('/conexoes', webBaseUrl).toString(),
  financeHubBase: webBaseUrl,
  financeHubApi: apiBaseUrl,
  pluggyAccounts: `${apiBaseUrl}${RUNTIME_CONSTANTS.pluggyAccountsPath}`,
} as const;

export function isMeuPluggyHost(url: string | undefined): boolean {
  try {
    if (!url) return false;
    return new URL(url).hostname === RUNTIME_CONSTANTS.pluggyHost;
  } catch {
    return false;
  }
}

export function isFinanceHubHost(url: string | undefined): boolean {
  try {
    if (!url) return false;
    const activeUrl = new URL(url);
    const targetUrl = new URL(RUNTIME_URLS.financeHubBase);

    // Exact origin match (e.g. http://localhost:3000 or https://financehub.app)
    if (activeUrl.origin === targetUrl.origin) {
      return true;
    }

    // Localhost development variants (localhost, 127.0.0.1 on configured or standard dev ports)
    const isLocalActive = activeUrl.hostname === 'localhost' || activeUrl.hostname === '127.0.0.1';
    const isLocalTarget = targetUrl.hostname === 'localhost' || targetUrl.hostname === '127.0.0.1';

    if (isLocalActive && isLocalTarget) {
      return (
        activeUrl.port === targetUrl.port ||
        activeUrl.port === '3000' ||
        activeUrl.port === '5173' ||
        targetUrl.port === '3000' ||
        targetUrl.port === '5173'
      );
    }

    return activeUrl.hostname === targetUrl.hostname;
  } catch {
    return false;
  }
}

export function isTrustedSite(url: string | undefined): boolean {
  return isMeuPluggyHost(url) || isFinanceHubHost(url);
}
