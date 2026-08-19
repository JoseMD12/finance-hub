import { isPluggyAccountList, type PluggyAccount } from '@financehub/web-shared';
import { RUNTIME_CONSTANTS, RUNTIME_URLS } from '../constants/runtime';

export class AccountsApiError extends Error {
  public constructor(public readonly status: number) {
    super('Não foi possível consultar as contas conectadas.');
    this.name = 'AccountsApiError';
  }
}

export async function getConnectedAccounts(token: string): Promise<PluggyAccount[]> {
  const controller = new AbortController();
  const timeoutId = window.setTimeout(() => controller.abort(), RUNTIME_CONSTANTS.requestTimeoutMs);

  try {
    const response = await fetch(RUNTIME_URLS.pluggyAccounts, {
      headers: {
        Accept: 'application/json',
        [RUNTIME_CONSTANTS.pluggyAccessTokenHeader]: token,
      },
      signal: controller.signal,
    });

    if (!response.ok) throw new AccountsApiError(response.status);
    const payload: unknown = await response.json();
    return isPluggyAccountList(payload) ? payload : [];
  } catch (error) {
    if (error instanceof AccountsApiError) throw error;
    throw new AccountsApiError(0);
  } finally {
    window.clearTimeout(timeoutId);
  }
}
