import { describe, it, expect, beforeEach } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { usePluggyToken } from './usePluggyToken';
import { CONNECTIONS_STORAGE_KEYS } from '../constants/connectionsConstants';

describe('usePluggyToken Hook', () => {
  beforeEach(() => {
    sessionStorage.clear();
  });

  it('initializes with empty token and without active session', () => {
    const { result } = renderHook(() => usePluggyToken());
    expect(result.current.token).toBe('');
    expect(result.current.hasToken).toBe(false);
    expect(result.current.lastSync).toBeNull();
  });

  it('saves token and updates sessionStorage and active state', () => {
    const { result } = renderHook(() => usePluggyToken());

    act(() => {
      result.current.saveToken('test-access-token-123');
    });

    expect(result.current.token).toBe('test-access-token-123');
    expect(result.current.hasToken).toBe(true);
    expect(sessionStorage.getItem(CONNECTIONS_STORAGE_KEYS.ACCESS_TOKEN)).toBe('test-access-token-123');
  });

  it('clears token and removes from sessionStorage', () => {
    const { result } = renderHook(() => usePluggyToken());

    act(() => {
      result.current.saveToken('test-access-token-123');
    });
    expect(result.current.hasToken).toBe(true);

    act(() => {
      result.current.clearToken();
    });

    expect(result.current.token).toBe('');
    expect(result.current.hasToken).toBe(false);
    expect(sessionStorage.getItem(CONNECTIONS_STORAGE_KEYS.ACCESS_TOKEN)).toBeNull();
  });

  it('clears the previous sync summary when a different token is saved', () => {
    const { result } = renderHook(() => usePluggyToken());
    const summary = {
      totalItemsSynced: 1,
      totalAccountsSynced: 2,
      totalCheckingTransactionsIngested: 3,
      totalCardTransactionsIngested: 4,
      syncedAtUtc: '2026-08-19T12:00:00Z',
    };

    act(() => {
      result.current.saveToken('first-token');
      result.current.saveLastSync(summary);
      result.current.saveToken('second-token');
    });

    expect(result.current.lastSync).toBeNull();
    expect(sessionStorage.getItem(CONNECTIONS_STORAGE_KEYS.LAST_SYNC)).toBeNull();
  });
});
