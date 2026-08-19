export const STORAGE_KEYS = {
  pluggyToken: 'pluggyToken',
  lastSync: 'lastSync',
  logoutLockUntil: 'financehub.logout-lock-until',
} as const;

export interface SessionStorageState {
  readonly pluggyToken?: string;
  readonly lastSync?: string;
  readonly logoutLockUntil?: number;
}
