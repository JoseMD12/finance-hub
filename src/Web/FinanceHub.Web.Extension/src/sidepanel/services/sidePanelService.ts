import { RUNTIME_CONSTANTS, RUNTIME_URLS } from '../../shared/constants/runtime';
import { browser } from 'wxt/browser';

export async function openFinanceHub(token: string | null): Promise<void> {
  const tab = await browser.tabs.create({ url: RUNTIME_URLS.financeHubWeb });
  if (!token || tab.id === undefined) return;

  const tokenToTransfer = token;
  const handleTabUpdated = async (tabId: number, changeInfo: { status?: string }) => {
    if (tabId !== tab.id || changeInfo.status !== 'complete') return;
    browser.tabs.onUpdated.removeListener(handleTabUpdated);

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
      // The FinanceHub tab remains available even when session transfer fails.
    }
  };

  browser.tabs.onUpdated.addListener(handleTabUpdated);
}

export function scheduleSidePanelClose(): void {
  window.setTimeout(async () => {
    if (!browser.sidePanel.close) return;
    const currentWindow = await browser.windows.getCurrent();
    if (currentWindow.id !== undefined) {
      await browser.sidePanel.close({ windowId: currentWindow.id });
    }
  }, RUNTIME_CONSTANTS.sidePanelCloseDelayMs);
}
