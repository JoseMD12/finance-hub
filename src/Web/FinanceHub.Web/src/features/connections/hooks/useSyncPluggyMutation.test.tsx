import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHook, act, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import React from 'react';
import { useSyncPluggyMutation } from './useSyncPluggyMutation';
import * as connectionsApi from '../api/connectionsApi';

vi.mock('sonner', () => ({
  toast: {
    info: vi.fn(),
    success: vi.fn(),
    error: vi.fn(),
  },
}));

describe('useSyncPluggyMutation Hook', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    vi.clearAllMocks();
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
        mutations: { retry: false },
      },
    });
  });

  const wrapper = ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );

  it('polls job status until Completed and calls onSyncSuccess with summary', async () => {
    const onSyncSuccess = vi.fn();

    vi.spyOn(connectionsApi, 'syncPluggyAccountsApi').mockResolvedValueOnce({
      jobId: 'job-123',
      status: 'Processing',
      message: 'Sincronização iniciada.',
      startedAtUtc: '2026-08-20T20:00:00Z',
    });

    vi.spyOn(connectionsApi, 'getSyncJobStatusApi')
      .mockResolvedValueOnce({
        jobId: 'job-123',
        status: 'Processing',
        message: 'Ainda processando.',
        startedAtUtc: '2026-08-20T20:00:00Z',
      })
      .mockResolvedValueOnce({
        jobId: 'job-123',
        status: 'Completed',
        message: 'Concluído.',
        startedAtUtc: '2026-08-20T20:00:00Z',
        completedAtUtc: '2026-08-20T20:00:02Z',
        result: {
          totalItemsSynced: 3,
          totalAccountsSynced: 6,
          totalCheckingTransactionsIngested: 800,
          totalCardTransactionsIngested: 600,
          syncedAtUtc: '2026-08-20T20:00:02Z',
        },
      });

    const { result } = renderHook(() => useSyncPluggyMutation({ onSyncSuccess }), {
      wrapper,
    });

    act(() => {
      result.current.mutate('valid-token');
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true), { timeout: 4000 });

    expect(onSyncSuccess).toHaveBeenCalledWith(
      expect.objectContaining({
        totalItemsSynced: 3,
        totalAccountsSynced: 6,
        totalCheckingTransactionsIngested: 800,
        totalCardTransactionsIngested: 600,
      })
    );
  });
});
