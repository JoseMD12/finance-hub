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

async function findExistingFinanceHubTab(): Promise<BrowserTab | undefined> {
  try {
    const tabs = (await browser.tabs.query({})) as BrowserTab[];
    return tabs.find((t) => isFinanceHubHost(t.url));
  } catch {
    return undefined;
  }
}

async function focusTabWindow(windowId?: number): Promise<void> {
  if (windowId === undefined) return;
  try {
    await browser.windows.update(windowId, { focused: true });
  } catch {
    // Best-effort window focus
  }
}

async function injectTokenIntoTab(tabId: number, token: string): Promise<void> {
  try {
    await browser.scripting.executeScript({
      target: { tabId },
      world: 'MAIN',
      func: (storageKey: string, sessionToken: string) => {
        window.sessionStorage.setItem(storageKey, sessionToken);
      },
      args: [RUNTIME_CONSTANTS.financeHubTokenStorageKey, token],
    });
    await browser.tabs.reload(tabId);
  } catch {
    // Best-effort script execution
  }
}

function listenTabCompleteAndInjectToken(tabId: number, token: string): void {
  const handleTabUpdated = async (updatedTabId: number, changeInfo: { status?: string }) => {
    if (updatedTabId !== tabId || changeInfo.status !== 'complete') return;
    browser.tabs.onUpdated.removeListener(handleTabUpdated);
    await injectTokenIntoTab(tabId, token);
  };
  browser.tabs.onUpdated.addListener(handleTabUpdated);
}

async function setupTokenInjection(tab: BrowserTab, tabId: number, targetUrl: string, token: string): Promise<void> {
  if (tab.url === targetUrl && tab.status === 'complete') {
    await injectTokenIntoTab(tabId, token);
  } else {
    listenTabCompleteAndInjectToken(tabId, token);
  }
}

export async function openFinanceHub(token?: string | null): Promise<void> {
  const sessionState = await getSessionState();
  const tokenToTransfer = token ?? sessionState.pluggyToken ?? null;
  const targetUrl = RUNTIME_URLS.financeHubWeb;

  const existingTab = await findExistingFinanceHubTab();

  if (existingTab?.id !== undefined) {
    const tabId = existingTab.id;
    await browser.tabs.update(tabId, { active: true, url: targetUrl });
    await focusTabWindow(existingTab.windowId);

    if (tokenToTransfer) {
      await setupTokenInjection(existingTab, tabId, targetUrl, tokenToTransfer);
    }
    return;
  }

  const tab = (await browser.tabs.create({ url: targetUrl })) as BrowserTab;
  if (tab?.id !== undefined && tokenToTransfer) {
    listenTabCompleteAndInjectToken(tab.id, tokenToTransfer);
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
