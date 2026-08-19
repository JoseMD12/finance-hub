import { defineContentScript } from 'wxt/utils/define-content-script';
import { browser } from 'wxt/browser';
import { MESSAGE_TYPES } from '../shared/contracts/messages';
import { RUNTIME_CONSTANTS } from '../shared/constants/runtime';
import { isJwtShape } from '../shared/security/token';

const PAGE_TOKEN_EVENT = 'FINANCEHUB_PLUGGY_TOKEN_INTERCEPTED';

export default defineContentScript({
  matches: ['https://meu.pluggy.ai/*'],
  runAt: 'document_idle',
  main() {
    let lastPath = window.location.pathname;

    const notifyLogout = () => {
      void browser.runtime.sendMessage({ type: MESSAGE_TYPES.logoutDetected });
    };

    const isLoginPath = () => RUNTIME_CONSTANTS.loginPathMarkers.some((marker) => {
      const path = window.location.pathname.toLowerCase();
      return path === marker || path.startsWith(`${marker}/`);
    });

    const inspectAuthenticationState = () => {
      if (window.location.pathname === lastPath) return;
      lastPath = window.location.pathname;
      if (isLoginPath()) notifyLogout();
    };

    const getInteractiveTarget = (event: Event): Element | null => {
      const path = typeof event.composedPath === 'function' ? event.composedPath() : [];
      const pathTarget = path.find((entry): entry is Element => entry instanceof Element
        && entry.matches('button, a, [role="button"]'));
      if (pathTarget) return pathTarget;
      return event.target instanceof Element
        ? event.target.closest('button, a, [role="button"]')
        : null;
    };

    document.addEventListener('click', (event) => {
      const target = getInteractiveTarget(event);
      const label = [
        target?.textContent,
        target?.getAttribute('aria-label'),
        target?.getAttribute('title'),
        target instanceof HTMLElement ? target.dataset.testid : undefined,
      ].filter(Boolean).join(' ').replace(/\s+/g, ' ');
      if (label && RUNTIME_CONSTANTS.logoutTextPattern.test(label)) notifyLogout();
    }, true);

    if (isLoginPath()) notifyLogout();
    window.setInterval(inspectAuthenticationState, 1_000);
    installPageObserver();

    window.addEventListener('message', (event) => {
      if (event.source !== window || event.origin !== window.location.origin) return;
      if (event.data?.type !== PAGE_TOKEN_EVENT || !isJwtShape(event.data.token)) return;
      void browser.runtime.sendMessage({
        type: MESSAGE_TYPES.tokenCaptured,
        token: event.data.token,
      });
    });
  },
});

function installPageObserver(): void {
  const script = document.createElement('script');
  script.textContent = `(${pageObserver.toString()})('${PAGE_TOKEN_EVENT}')`;
  (document.head || document.documentElement).appendChild(script);
  script.remove();
}

function pageObserver(eventName: string): void {
  const isJwtShapeInPage = (value: unknown): value is string => typeof value === 'string'
    && value.trim().length > 30
    && value.split('.').length === 3;

  const publish = (value: unknown) => {
    if (isJwtShapeInPage(value)) {
      window.postMessage({ type: eventName, token: value }, window.location.origin);
    }
  };

  const originalFetch = window.fetch;
  window.fetch = async function patchedFetch(...args) {
    const response = await originalFetch.apply(this, args);
    try {
      const clone = response.clone();
      if (clone.headers.get('content-type')?.includes('application/json')) {
        const data = await clone.json() as Record<string, unknown>;
        publish(data.accessToken || data.token || data.userToken || data.apiKey);
      }
    } catch {
      // Ignore responses that are not readable JSON.
    }
    return response;
  };

  const originalOpen = XMLHttpRequest.prototype.open;
  const originalSend = XMLHttpRequest.prototype.send;
  XMLHttpRequest.prototype.open = function patchedOpen(
    this: XMLHttpRequest,
    method: string,
    url: string | URL,
    async: boolean = true,
    username?: string | null,
    password?: string | null,
  ) {
    return originalOpen.call(this, method, url, async, username, password);
  } as typeof XMLHttpRequest.prototype.open;
  XMLHttpRequest.prototype.send = function patchedSend(body) {
    this.addEventListener('load', () => {
      try {
        const data = JSON.parse(this.responseText) as Record<string, unknown>;
        publish(data.accessToken || data.token || data.userToken || data.apiKey);
      } catch {
        // Ignore responses that are not JSON.
      }
    });
    return originalSend.call(this, body);
  };
}
