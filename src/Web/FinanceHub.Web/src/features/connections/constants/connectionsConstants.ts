export const CONNECTIONS_STORAGE_KEYS = {
  ACCESS_TOKEN: 'pluggy_access_token',
  LAST_SYNC: 'pluggy_last_sync_summary',
} as const;

export const CONNECTIONS_DEFAULTS = {
  STALE_TIME_MS: 120000,
  DEFAULT_BADGE: 'Meu.Pluggy Open Finance',
  OFFLINE_ACCEPTED_FORMATS: '.ofx, .csv, .pdf',
  PLUGGY_PORTAL_URL: 'https://meu.pluggy.ai',
} as const;
