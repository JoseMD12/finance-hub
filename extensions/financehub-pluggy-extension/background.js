// Background Service Worker para a extensão do FinanceHub
const PLUGGY_HOST = 'meu.pluggy.ai';
const LOGOUT_LOCK_KEY = 'pluggyLogoutDetectedAt';
const LOGOUT_LOCK_DURATION_MS = 5000;
let logoutLockUntil = 0;

chrome.runtime.onInstalled.addListener(() => {
  console.log('[FinanceHub Extension] Extensão instalada com sucesso.');
});

chrome.action.onClicked.addListener(async (tab) => {
  if (!tab.id || !isMeuPluggyUrl(tab.url)) {
    return;
  }

  try {
    await chrome.sidePanel.open({ tabId: tab.id });
  } catch (error) {
    console.error('[FinanceHub Extension] Não foi possível abrir o painel lateral.', error);
  }
});

function isMeuPluggyUrl(url) {
  try {
    return new URL(url).hostname === PLUGGY_HOST;
  } catch {
    return false;
  }
}

// Escuta requisições de rede enviadas com o cabeçalho Authorization
chrome.webRequest.onBeforeSendHeaders.addListener(
  (details) => {
    if (details.requestHeaders) {
      for (const header of details.requestHeaders) {
        if (header.name.toLowerCase() === 'authorization' && header.value?.startsWith('Bearer ')) {
          const token = header.value.replace('Bearer ', '').trim();
          if (token.split('.').length === 3) {
            chrome.storage.local.get(LOGOUT_LOCK_KEY, (result) => {
              const logoutDetectedAt = Number(result[LOGOUT_LOCK_KEY] || 0);
              const isLocked = Date.now() < logoutLockUntil
                || (logoutDetectedAt > 0 && Date.now() - logoutDetectedAt < LOGOUT_LOCK_DURATION_MS);

              if (isLocked) return;

              console.log('[FinanceHub Extension] Token de rede (Bearer) interceptado!');
              chrome.storage.local.set({ pluggyToken: token, lastSync: new Date().toISOString() });
            });
          }
        }
      }
    }
  },
  { urls: ['https://my-api.pluggy.ai/*', 'https://api.pluggy.ai/*', 'https://meu.pluggy.ai/*'] },
  ['requestHeaders']
);

// Escuta mensagens capturadas pelo content.js
chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
  if (message.action === 'tokenCaptured' && message.token) {
    logoutLockUntil = 0;
    chrome.storage.local.remove(LOGOUT_LOCK_KEY, () => {
      chrome.storage.local.set({ pluggyToken: message.token, lastSync: new Date().toISOString() }, () => {
        console.log('[FinanceHub Extension] Token salvo com sucesso no storage!');
        sendResponse({ status: 'success' });
      });
    });
    return true;
  }

  if (message.action === 'getToken') {
    chrome.storage.local.get(['pluggyToken', 'lastSync'], (result) => {
      sendResponse(result);
    });
    return true;
  }

  if (message.action === 'logoutDetected') {
    logoutLockUntil = Date.now() + LOGOUT_LOCK_DURATION_MS;
    chrome.storage.local.set({ [LOGOUT_LOCK_KEY]: Date.now() });
    chrome.storage.local.remove(['pluggyToken', 'lastSync']);
    sendResponse({ status: 'cleared' });
    return false;
  }
});
