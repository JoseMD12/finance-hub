import { useEffect, useState } from 'react';
import { browser } from 'wxt/browser';
import type { PluggyAccount } from '@financehub/web-shared';
import { getConnectedAccounts } from '../shared/api/pluggyAccountsApi';
import { isFinanceHubHost, isMeuPluggyHost } from '../shared/constants/runtime';
import { STORAGE_KEYS } from '../shared/contracts/storage';
import { decodeDisplayIdentity, type DisplayIdentity } from '../shared/security/token';
import { getSessionState } from '../shared/storage/sessionStorage';
import { AccountsSection } from './components/AccountsSection';
import { BrandHeader } from './components/BrandHeader';
import { NavigationActions } from './components/NavigationActions';
import { TokenCard } from './components/TokenCard';
import { UserCard } from './components/UserCard';
import { openFinanceHub, openMeuPluggy, scheduleSidePanelClose } from './services/sidePanelService';

export function SidePanelApp() {
  const [isOnPluggySite, setIsOnPluggySite] = useState<boolean>(false);
  const [isOnFinanceHubSite, setIsOnFinanceHubSite] = useState<boolean>(false);
  const [token, setToken] = useState<string | null>(null);
  const [identity, setIdentity] = useState<DisplayIdentity | null>(null);
  const [accounts, setAccounts] = useState<PluggyAccount[]>([]);
  const [isLoadingAccounts, setIsLoadingAccounts] = useState(false);
  const [hasAccountsError, setHasAccountsError] = useState(false);
  const [copied, setCopied] = useState(false);

  useEffect(() => {
    let requestVersion = 0;

    const checkTabAndApplyToken = async (tokenFromStorage?: string | null) => {
      requestVersion += 1;
      const currentRequest = requestVersion;

      let activeUrl: string | undefined;
      try {
        const [activeTab] = await browser.tabs.query({ active: true, currentWindow: true });
        activeUrl = activeTab?.url;
      } catch {
        activeUrl = undefined;
      }

      const onPluggy = isMeuPluggyHost(activeUrl);
      const onFinanceHub = isFinanceHubHost(activeUrl);
      const isTrusted = onPluggy || onFinanceHub;

      setIsOnPluggySite(onPluggy);
      setIsOnFinanceHubSite(onFinanceHub);

      if (!isTrusted) {
        setToken(null);
        setIdentity(null);
        setAccounts([]);
        setHasAccountsError(false);
        setIsLoadingAccounts(false);
        return;
      }

      const nextToken =
        tokenFromStorage !== undefined
          ? tokenFromStorage
          : (await getSessionState()).pluggyToken || null;

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

    void checkTabAndApplyToken();

    const handleStorageChange = (changes: Record<string, { newValue?: unknown }>, areaName: string) => {
      if (areaName !== 'local' || !Object.hasOwn(changes, STORAGE_KEYS.pluggyToken)) return;
      const nextToken = changes[STORAGE_KEYS.pluggyToken]?.newValue;
      void checkTabAndApplyToken(typeof nextToken === 'string' ? nextToken : null);
    };

    const handleTabActivated = () => {
      void checkTabAndApplyToken();
    };

    const handleTabUpdated = (_tabId: number, changeInfo: { url?: string; status?: string }) => {
      if (changeInfo.url || changeInfo.status === 'complete') {
        void checkTabAndApplyToken();
      }
    };

    browser.storage.onChanged.addListener(handleStorageChange);
    browser.tabs.onActivated.addListener(handleTabActivated);
    browser.tabs.onUpdated.addListener(handleTabUpdated);

    return () => {
      browser.storage.onChanged.removeListener(handleStorageChange);
      browser.tabs.onActivated.removeListener(handleTabActivated);
      browser.tabs.onUpdated.removeListener(handleTabUpdated);
    };
  }, []);

  const handleCopy = () => {
    if (!token) return;
    void navigator.clipboard.writeText(token).then(() => {
      setCopied(true);
      window.setTimeout(() => setCopied(false), 1_500);
    });
  };

  const handleOpenMeuPluggy = () => {
    void openMeuPluggy();
  };

  const handleOpenFinanceHub = () => {
    scheduleSidePanelClose();
    void openFinanceHub(token);
  };

  const isTrustedSite = isOnPluggySite || isOnFinanceHubSite;

  return (
    <main className="side-panel-shell">
      <BrandHeader />
      {isTrustedSite && identity && <UserCard identity={identity} />}
      <TokenCard
        hasToken={isTrustedSite && Boolean(token)}
        onCopy={handleCopy}
        copied={copied}
        customHelpText={!isTrustedSite ? 'Navegue para o Meu.Pluggy ou FinanceHub para visualizar o token.' : undefined}
      />
      {isTrustedSite && token && (
        <AccountsSection accounts={accounts} isLoading={isLoadingAccounts} hasError={hasAccountsError} />
      )}
      <NavigationActions
        onOpenMeuPluggy={handleOpenMeuPluggy}
        onOpenFinanceHub={handleOpenFinanceHub}
        isOnPluggySite={isOnPluggySite}
        isOnFinanceHubSite={isOnFinanceHubSite}
      />
    </main>
  );
}
