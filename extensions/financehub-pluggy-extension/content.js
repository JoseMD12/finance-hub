// Content script para meu.pluggy.ai — Interceptação de Rede & Injeção no Main World
(function () {
  console.log('[FinanceHub Extension] Content script de interceptação ativado em meu.pluggy.ai');

  // Injeta um script no contexto principal da página (Main World) para interceptar requisições fetch/XHR
  const scriptNode = document.createElement('script');
  scriptNode.textContent = `
    (function() {
      function isValidToken(val) {
        return typeof val === 'string' && val.trim().length > 30 && val.split('.').length === 3;
      }

      // 1. Intercepta requisições fetch
      const originalFetch = window.fetch;
      window.fetch = async function(...args) {
        const response = await originalFetch.apply(this, args);
        try {
          const clone = response.clone();
          const contentType = clone.headers.get('content-type');
          if (contentType && contentType.includes('application/json')) {
            const data = await clone.json();
            const foundToken = data.accessToken || data.token || data.userToken || data.apiKey;
            if (isValidToken(foundToken)) {
              console.log('[FinanceHub Extension] 🔑 AccessToken interceptado via fetch');
              window.postMessage({ type: 'FINANCEHUB_PLUGGY_TOKEN_INTERCEPTED', token: foundToken }, window.location.origin);
            }
          }
        } catch(e) {}
        return response;
      };

      // 2. Intercepta XMLHttpRequest
      const originalOpen = XMLHttpRequest.prototype.open;
      const originalSend = XMLHttpRequest.prototype.send;
      XMLHttpRequest.prototype.open = function(method, url) {
        this._url = url;
        return originalOpen.apply(this, arguments);
      };
      XMLHttpRequest.prototype.send = function() {
        this.addEventListener('load', function() {
          try {
            if (this.responseText && this.responseText.includes('accessToken')) {
              const json = JSON.parse(this.responseText);
              const foundToken = json.accessToken || json.token || json.userToken;
              if (isValidToken(foundToken)) {
                console.log('[FinanceHub Extension] 🔑 AccessToken interceptado via XHR');
                window.postMessage({ type: 'FINANCEHUB_PLUGGY_TOKEN_INTERCEPTED', token: foundToken }, window.location.origin);
              }
            }
          } catch(e) {}
        });
        return originalSend.apply(this, arguments);
      };
    })();
  `;
  (document.head || document.documentElement).appendChild(scriptNode);

  // Escuta mensagens vindas do script injetado no Main World
  window.addEventListener('message', (event) => {
    if (event.source !== window || (event.origin && event.origin !== window.location.origin)) {
      return;
    }

    if (event.data?.type === 'FINANCEHUB_PLUGGY_TOKEN_INTERCEPTED' && event.data?.token) {
      const token = event.data.token;
      console.log('[FinanceHub Extension] Transmitindo token capturado...');
      chrome.runtime.sendMessage({ action: 'tokenCaptured', token: token });
      window.postMessage({ type: 'FINANCEHUB_PLUGGY_TOKEN', token: token }, window.location.origin);
    }
  });
})();
