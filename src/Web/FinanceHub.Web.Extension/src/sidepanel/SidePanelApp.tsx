import { useEffect, useState } from 'react';
import { browser } from 'wxt/browser';
import type { PluggyAccount } from '@financehub/web-shared';
import { getConnectedAccounts } from '../shared/api/pluggyAccountsApi';
import { STORAGE_KEYS } from '../shared/contracts/storage';
import { decodeDisplayIdentity, type DisplayIdentity } from '../shared/security/token';
import { getSessionState } from '../shared/storage/sessionStorage';
import { AccountsSection } from './components/AccountsSection';
import { BrandHeader } from './components/BrandHeader';
import { FinanceHubButton } from './components/FinanceHubButton';
import { TokenCard } from './components/TokenCard';
import { UserCard } from './components/UserCard';
import { openFinanceHub, scheduleSidePanelClose } from './services/sidePanelService';

export function SidePanelApp() {
  const [token, setToken] = useState<string | null>(null);
  const [identity, setIdentity] = useState<DisplayIdentity | null>(null);
  const [accounts, setAccounts] = useState<PluggyAccount[]>([]);
  const [isLoadingAccounts, setIsLoadingAccounts] = useState(false);
  const [hasAccountsError, setHasAccountsError] = useState(false);
  const [copied, setCopied] = useState(false);

  useEffect(() => {
    let requestVersion = 0;

    const applyToken = (nextToken: string | null) => {
      requestVersion += 1;
      const currentRequest = requestVersion;
      setToken(nextToken);
      setIdentity(nextToken ? decodeDisplayIdentity(nextToken) : null);
      setAccounts([]);
      setHasAccountsError(false);

      if (!nextToken) {
        setIsLoadingAccounts(false);
        return;
      }

      setIsLoadingAccounts(true);
      void getConnectedAccounts(nextToken)
        .then((nextAccounts) => {
          if (currentRequest === requestVersion) setAccounts(nextAccounts);
        })
        .catch(() => {
          if (currentRequest === requestVersion) setHasAccountsError(true);
        })
        .finally(() => {
          if (currentRequest === requestVersion) setIsLoadingAccounts(false);
        });
    };

    void getSessionState().then((state) => applyToken(state.pluggyToken || null));
    const handleStorageChange = (changes: Record<string, { newValue?: unknown }>, areaName: string) => {
      if (areaName !== 'local' || !Object.prototype.hasOwnProperty.call(changes, STORAGE_KEYS.pluggyToken)) return;
      const nextToken = changes[STORAGE_KEYS.pluggyToken]?.newValue;
      applyToken(typeof nextToken === 'string' ? nextToken : null);
    };
    browser.storage.onChanged.addListener(handleStorageChange);
    return () => browser.storage.onChanged.removeListener(handleStorageChange);
  }, []);

  const handleCopy = () => {
    if (!token) return;
    void navigator.clipboard.writeText(token).then(() => {
      setCopied(true);
      window.setTimeout(() => setCopied(false), 1_500);
    });
  };

  const handleOpenFinanceHub = () => {
    scheduleSidePanelClose();
    void openFinanceHub(token);
  };

  return (
    <main className="side-panel-shell">
      <BrandHeader />
      {identity && <UserCard identity={identity} />}
      <TokenCard hasToken={Boolean(token)} onCopy={handleCopy} copied={copied} />
      {token && <AccountsSection accounts={accounts} isLoading={isLoadingAccounts} hasError={hasAccountsError} />}
      <FinanceHubButton onClick={handleOpenFinanceHub} />
    </main>
  );
}
