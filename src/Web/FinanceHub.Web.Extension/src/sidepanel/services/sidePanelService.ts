import { RUNTIME_CONSTANTS, RUNTIME_URLS, isFinanceHubHost } from '../../shared/constants/runtime';
import { getSessionState } from '../../shared/storage/sessionStorage';
import { browser } from 'wxt/browser';

interface BrowserTab {
  id?: number;
  url?: string;
  windowId?: number;
  status?: string;
}

export async function openMeuPluggy(): Promise<void> {
  await browser.tabs.create({ url: RUNTIME_URLS.meuPluggy });
}

export async function openFinanceHub(token?: string | null): Promise<void> {
  const sessionState = await getSessionState();
  const tokenToTransfer = token ?? sessionState.pluggyToken ?? null;
  const targetUrl = RUNTIME_URLS.financeHubWeb;

  let existingTab: BrowserTab | undefined;
  try {
    const tabs = (await browser.tabs.query({})) as BrowserTab[];
    existingTab = tabs.find((t) => isFinanceHubHost(t.url));
  } catch {
    existingTab = undefined;
  }

  const injectToken = async (tabId: number) => {
    if (!tokenToTransfer) return;
    try {
      await browser.scripting.executeScript({
        target: { tabId },
        world: 'MAIN',
        func: (storageKey: string, sessionToken: string) => {
          window.sessionStorage.setItem(storageKey, sessionToken);
        },
        args: [RUNTIME_CONSTANTS.financeHubTokenStorageKey, tokenToTransfer],
      });
      await browser.tabs.reload(tabId);
    } catch {
      // Best-effort script execution
    }
  };

  if (existingTab?.id !== undefined) {
    const tabId = existingTab.id;
    await browser.tabs.update(tabId, { active: true, url: targetUrl });
    if (existingTab.windowId !== undefined) {
      try {
        await browser.windows.update(existingTab.windowId, { focused: true });
      } catch {
        // Best-effort window focus
      }
    }

    if (tokenToTransfer) {
      if (existingTab.url === targetUrl && existingTab.status === 'complete') {
        await injectToken(tabId);
      } else {
        const handleTabUpdated = async (updatedTabId: number, changeInfo: { status?: string }) => {
          if (updatedTabId !== tabId || changeInfo.status !== 'complete') return;
          browser.tabs.onUpdated.removeListener(handleTabUpdated);
          await injectToken(tabId);
        };
        browser.tabs.onUpdated.addListener(handleTabUpdated);
      }
    }
  } else {
    const tab = (await browser.tabs.create({ url: targetUrl })) as BrowserTab;
    if (tab?.id !== undefined && tokenToTransfer) {
      const tabId = tab.id;
      const handleTabUpdated = async (updatedTabId: number, changeInfo: { status?: string }) => {
        if (updatedTabId !== tabId || changeInfo.status !== 'complete') return;
        browser.tabs.onUpdated.removeListener(handleTabUpdated);
        await injectToken(tabId);
      };
      browser.tabs.onUpdated.addListener(handleTabUpdated);
    }
  }
}

export function scheduleSidePanelClose(): void {
  setTimeout(async () => {
    if (!browser.sidePanel?.close) return;
    try {
      const currentWindow = await browser.windows.getCurrent();
      if (currentWindow.id !== undefined) {
        await browser.sidePanel.close({ windowId: currentWindow.id });
      }
    } catch {
      // Best-effort side panel close
    }
  }, RUNTIME_CONSTANTS.sidePanelCloseDelayMs);
}
