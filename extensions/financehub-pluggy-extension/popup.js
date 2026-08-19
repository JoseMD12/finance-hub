const PLUGGY_ACCOUNTS_URL = 'http://localhost:5056/api/v1/pluggy/accounts';
const PLUGGY_ACCESS_TOKEN_HEADER = 'x-pluggy-access-token';
const FINANCEHUB_CONNECTIONS_URL = 'http://localhost:3000/conexoes';
const FINANCEHUB_TOKEN_STORAGE_KEY = 'pluggy_access_token';
const SIDE_PANEL_CLOSE_DELAY_MS = 2500;

function closeSidePanelAfterDelay() {
  window.setTimeout(async () => {
    if (!chrome.sidePanel?.close) return;

    try {
      const currentWindow = await chrome.windows.getCurrent();
      if (currentWindow.id !== undefined) {
        await chrome.sidePanel.close({ windowId: currentWindow.id });
      }
    } catch (error) {
      console.warn('[FinanceHub Extension] Não foi possível fechar o Side Panel.', error);
    }
  }, SIDE_PANEL_CLOSE_DELAY_MS);
}

document.addEventListener('DOMContentLoaded', () => {
  const cardContainer = document.getElementById('cardContainer');
  const tokenTitle = document.getElementById('tokenTitle');
  const tokenSubtext = document.getElementById('tokenSubtext');
  const copyBtn = document.getElementById('copyBtn');
  const userSection = document.getElementById('userSection');
  const userAvatar = document.getElementById('userAvatar');
  const userName = document.getElementById('userName');
  const userEmail = document.getElementById('userEmail');
  const accountsSection = document.getElementById('accountsSection');
  const accountsList = document.getElementById('accountsList');
  const openFinanceHubBtn = document.getElementById('openFinanceHubBtn');

  let activeToken = null;
  let accountsRequestId = 0;

  function parseTokenPayload(token) {
    try {
      const payload = token.split('.')[1];
      const normalized = payload.replace(/-/g, '+').replace(/_/g, '/');
      const padded = normalized.padEnd(Math.ceil(normalized.length / 4) * 4, '=');
      return JSON.parse(atob(padded));
    } catch {
      return {};
    }
  }

  function updateUser(token) {
    if (!token) {
      userSection.hidden = true;
      return;
    }

    const claims = parseTokenPayload(token);
    const email = claims.email || claims['https://api.pluggy.ai/email'] || 'Sessão identificada';
    const name = claims.name || claims.nameid || claims['https://api.pluggy.ai/name'] || email;

    userSection.hidden = false;
    userName.textContent = name;
    userEmail.textContent = email;
    userAvatar.textContent = String(name).trim().charAt(0).toUpperCase() || '?';
  }

  function accountType(account) {
    const subtype = String(account.subtype || '').toUpperCase();
    const type = String(account.type || '').toUpperCase();

    if (subtype.includes('CREDIT') || type === 'CREDIT') return 'Crédito';
    if (subtype.includes('CHECKING')) return 'Conta corrente';
    if (subtype.includes('SAVINGS')) return 'Poupança';
    if (subtype.includes('INVEST')) return 'Investimentos';
    return account.name || 'Conta';
  }

  function renderAccounts(accounts) {
    accountsList.replaceChildren();

    if (!accounts.length) {
      accountsList.innerHTML = '<div class="accounts-state">Nenhuma conta retornada pelo backend.</div>';
      return;
    }

    accounts.forEach((account) => {
      const row = document.createElement('div');
      row.className = 'account-row';

      const main = document.createElement('div');
      main.className = 'account-main';

      const institution = document.createElement('div');
      institution.className = 'account-institution';
      institution.textContent = account.institutionName || 'Instituição';

      const name = document.createElement('div');
      name.className = 'account-name';
      name.textContent = account.name || 'Conta conectada';

      const type = document.createElement('span');
      type.className = 'account-type';
      type.textContent = accountType(account);

      main.append(institution, name);
      row.append(main, type);
      accountsList.appendChild(row);
    });
  }

  async function loadAccounts(token) {
    const requestId = ++accountsRequestId;
    accountsSection.hidden = false;
    accountsList.innerHTML = '<div class="accounts-state">Consultando contas conectadas...</div>';

    try {
      const response = await fetch(PLUGGY_ACCOUNTS_URL, {
        headers: {
          Accept: 'application/json',
          [PLUGGY_ACCESS_TOKEN_HEADER]: token,
        },
      });

      if (!response.ok) {
        throw new Error(`Backend returned ${response.status}`);
      }

      const accounts = await response.json();
      if (requestId === accountsRequestId && activeToken === token) {
        renderAccounts(Array.isArray(accounts) ? accounts : []);
      }
    } catch (error) {
      console.warn('[FinanceHub Extension] Não foi possível carregar as contas do backend.', error);
      if (requestId === accountsRequestId && activeToken === token) {
        accountsList.innerHTML = '<div class="accounts-state">Não foi possível carregar as contas agora.</div>';
      }
    }
  }

  function updateUi(token) {
    activeToken = token || null;

    if (!activeToken) {
      cardContainer.classList.remove('found');
      tokenTitle.textContent = 'Nenhum Token';
      tokenSubtext.textContent = 'Nenhuma sessão disponível.';
      copyBtn.disabled = true;
      userSection.hidden = true;
      accountsSection.hidden = true;
      accountsRequestId += 1;
      return;
    }

    cardContainer.classList.add('found');
    tokenTitle.textContent = 'Token Encontrado';
    tokenSubtext.textContent = 'Copie o token para usar no FinanceHub.';
    copyBtn.disabled = false;
    updateUser(activeToken);
    loadAccounts(activeToken);
  }

  chrome.storage.local.get(['pluggyToken'], (result) => {
    updateUi(result.pluggyToken || null);
  });

  chrome.storage.onChanged.addListener((changes, areaName) => {
    if (areaName !== 'local' || !Object.prototype.hasOwnProperty.call(changes, 'pluggyToken')) {
      return;
    }

    updateUi(changes.pluggyToken.newValue || null);
  });

  copyBtn.addEventListener('click', () => {
    if (!activeToken) return;

    navigator.clipboard.writeText(activeToken).then(() => {
      copyBtn.innerHTML = `
        <svg viewBox="0 0 24 24" fill="none" stroke="#FFFFFF" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
          <polyline points="20 6 9 17 4 12"/>
        </svg>
      `;
      copyBtn.style.backgroundColor = '#E05697';
      copyBtn.style.color = '#FFFFFF';

      setTimeout(() => {
        copyBtn.innerHTML = `
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <rect width="14" height="14" x="8" y="8" rx="2"/>
            <path d="M4 16c-1.1 0-2-.9-2-2V4c0-1.1.9-2 2-2h10c1.1 0 2 .9 2 2"/>
          </svg>
        `;
        copyBtn.style.backgroundColor = '';
        copyBtn.style.color = '';
      }, 1500);
    });
  });

  openFinanceHubBtn.addEventListener('click', () => {
    closeSidePanelAfterDelay();

    if (!activeToken) {
      chrome.tabs.create({ url: FINANCEHUB_CONNECTIONS_URL });
      return;
    }

    const tokenToTransfer = activeToken;
    chrome.tabs.create({ url: FINANCEHUB_CONNECTIONS_URL }, (tab) => {
      if (!tab.id) return;

      const handleTabUpdated = (tabId, changeInfo) => {
        if (tabId !== tab.id || changeInfo.status !== 'complete') return;

        chrome.tabs.onUpdated.removeListener(handleTabUpdated);
        chrome.scripting.executeScript({
          target: { tabId: tab.id },
          world: 'MAIN',
          func: (storageKey, token) => {
            window.sessionStorage.setItem(storageKey, token);
          },
          args: [FINANCEHUB_TOKEN_STORAGE_KEY, tokenToTransfer],
        }).then(() => chrome.tabs.reload(tab.id)).catch((error) => {
          console.error('[FinanceHub Extension] Não foi possível transferir a sessão para o FinanceHub.', error);
        });
      };

      chrome.tabs.onUpdated.addListener(handleTabUpdated);
    });
  });
});
