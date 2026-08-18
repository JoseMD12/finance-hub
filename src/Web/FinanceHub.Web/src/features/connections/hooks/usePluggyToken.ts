import { useState, useEffect, useCallback } from 'react';
import { CONNECTIONS_STORAGE_KEYS } from '../constants/connectionsConstants';
import type { PluggySyncSummaryDto } from '../types/connections.types';

export function usePluggyToken() {
  const [token, setToken] = useState<string>('');
  const [lastSync, setLastSync] = useState<PluggySyncSummaryDto | null>(null);

  useEffect(() => {
    try {
      const savedToken = sessionStorage.getItem(CONNECTIONS_STORAGE_KEYS.ACCESS_TOKEN) || '';
      const savedSyncRaw = sessionStorage.getItem(CONNECTIONS_STORAGE_KEYS.LAST_SYNC);
      
      setToken(savedToken);
      if (savedSyncRaw) {
        setLastSync(JSON.parse(savedSyncRaw));
      }
    } catch {
      setToken('');
      setLastSync(null);
    }
  }, []);

  const saveToken = useCallback((newToken: string) => {
    const trimmed = newToken.trim();
    setToken(trimmed);
    if (trimmed) {
      sessionStorage.setItem(CONNECTIONS_STORAGE_KEYS.ACCESS_TOKEN, trimmed);
    } else {
      sessionStorage.removeItem(CONNECTIONS_STORAGE_KEYS.ACCESS_TOKEN);
    }
  }, []);

  const saveLastSync = useCallback((summary: PluggySyncSummaryDto) => {
    setLastSync(summary);
    try {
      sessionStorage.setItem(CONNECTIONS_STORAGE_KEYS.LAST_SYNC, JSON.stringify(summary));
    } catch {}
  }, []);

  const clearToken = useCallback(() => {
    setToken('');
    setLastSync(null);
    sessionStorage.removeItem(CONNECTIONS_STORAGE_KEYS.ACCESS_TOKEN);
    sessionStorage.removeItem(CONNECTIONS_STORAGE_KEYS.LAST_SYNC);
  }, []);

  return {
    token,
    hasToken: Boolean(token.trim()),
    lastSync,
    saveToken,
    saveLastSync,
    clearToken,
  };
}
