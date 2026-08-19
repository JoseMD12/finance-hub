import { STORAGE_KEYS, type SessionStorageState } from '../contracts/storage';
import { browser } from 'wxt/browser';

export async function getSessionState(): Promise<SessionStorageState> {
  const state = await browser.storage.local.get();
  return {
    pluggyToken: state[STORAGE_KEYS.pluggyToken] as string | undefined,
    lastSync: state[STORAGE_KEYS.lastSync] as string | undefined,
    logoutLockUntil: state[STORAGE_KEYS.logoutLockUntil] as number | undefined,
  };
}

export async function saveToken(token: string): Promise<void> {
  await browser.storage.local.set({
    [STORAGE_KEYS.pluggyToken]: token,
    [STORAGE_KEYS.lastSync]: new Date().toISOString(),
  });
}

export async function clearSession(): Promise<void> {
  await browser.storage.local.remove([
    STORAGE_KEYS.pluggyToken,
    STORAGE_KEYS.lastSync,
  ]);
}

export async function setLogoutLock(until: number): Promise<void> {
  await browser.storage.local.set({ [STORAGE_KEYS.logoutLockUntil]: until });
}
