// Background Service Worker para a extensão do FinanceHub
chrome.runtime.onInstalled.addListener(() => {
  console.log('[FinanceHub Extension] Extensão instalada com sucesso.');
});

// Escuta requisições de rede enviadas com o cabeçalho Authorization
chrome.webRequest.onBeforeSendHeaders.addListener(
  (details) => {
    if (details.requestHeaders) {
      for (const header of details.requestHeaders) {
        if (header.name.toLowerCase() === 'authorization' && header.value?.startsWith('Bearer ')) {
          const token = header.value.replace('Bearer ', '').trim();
          if (token.split('.').length === 3) {
            console.log('[FinanceHub Extension] Token de rede (Bearer) interceptado!');
            chrome.storage.local.set({ pluggyToken: token, lastSync: new Date().toISOString() });
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
    chrome.storage.local.set({ pluggyToken: message.token, lastSync: new Date().toISOString() }, () => {
      console.log('[FinanceHub Extension] Token salvo com sucesso no storage!');
      sendResponse({ status: 'success' });
    });
    return true;
  }

  if (message.action === 'getToken') {
    chrome.storage.local.get(['pluggyToken', 'lastSync'], (result) => {
      sendResponse(result);
    });
    return true;
  }
});
