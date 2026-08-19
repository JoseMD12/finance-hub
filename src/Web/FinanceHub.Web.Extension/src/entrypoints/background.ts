import { defineBackground } from 'wxt/utils/define-background';
import { browser } from 'wxt/browser';
import { MESSAGE_TYPES, isRuntimeMessage } from '../shared/contracts/messages';
import { RUNTIME_CONSTANTS } from '../shared/constants/runtime';
import { STORAGE_KEYS } from '../shared/contracts/storage';
import { clearSession, saveToken, setLogoutLock } from '../shared/storage/sessionStorage';
import { isJwtShape } from '../shared/security/token';

export default defineBackground(() => {
  let logoutLockUntil = 0;

  browser.action.onClicked.addListener(async (tab) => {
    if (!tab.id || !isMeuPluggyUrl(tab.url)) return;
    await browser.sidePanel.open({ tabId: tab.id });
  });

  browser.webRequest.onBeforeSendHeaders.addListener(
    (details) => {
      const authorization = details.requestHeaders?.find(
        (header) => header.name.toLowerCase() === 'authorization'
      )?.value;
      const token = authorization?.startsWith('Bearer ') ? authorization.slice(7).trim() : null;
      if (!isJwtShape(token) || isLogoutLocked()) return;

      void saveToken(token);
    },
    { urls: ['https://my-api.pluggy.ai/*', 'https://api.pluggy.ai/*', 'https://meu.pluggy.ai/*'] },
    ['requestHeaders']
  );

  browser.runtime.onMessage.addListener((message: unknown, _sender, sendResponse) => {
    if (!isRuntimeMessage(message)) return false;

    if (message.type === MESSAGE_TYPES.tokenCaptured) {
      logoutLockUntil = 0;
      void browser.storage.local.remove(STORAGE_KEYS.logoutLockUntil)
        .then(() => saveToken(message.token))
        .then(() => sendResponse({ status: 'success' }));
      return true;
    }

    logoutLockUntil = Date.now() + RUNTIME_CONSTANTS.logoutLockDurationMs;
    void setLogoutLock(logoutLockUntil).then(clearSession).then(() => sendResponse({ status: 'cleared' }));
    return true;
  });

  function isLogoutLocked(): boolean {
    return Date.now() < logoutLockUntil;
  }

  function isMeuPluggyUrl(url: string | undefined): boolean {
    try {
      return new URL(url ?? '').hostname === RUNTIME_CONSTANTS.pluggyHost;
    } catch {
      return false;
    }
  }
});
