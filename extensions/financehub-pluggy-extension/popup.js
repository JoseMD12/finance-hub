document.addEventListener('DOMContentLoaded', () => {
  const cardContainer = document.getElementById('cardContainer');
  const tokenTitle = document.getElementById('tokenTitle');
  const tokenSubtext = document.getElementById('tokenSubtext');
  const copyBtn = document.getElementById('copyBtn');

  let activeToken = null;

  function updateUi(token) {
    activeToken = token;

    if (token) {
      cardContainer.classList.add('found');
      tokenTitle.textContent = 'Token Encontrado';
      tokenSubtext.textContent = 'Clique no ícone ao lado para copiar.';
      copyBtn.disabled = false;
    } else {
      cardContainer.classList.remove('found');
      tokenTitle.textContent = 'Nenhum Token';
      tokenSubtext.textContent = 'Acesse o meu.pluggy.ai para autenticar.';
      copyBtn.disabled = true;
    }
  }

  // Busca o token no storage da extensão ao abrir o popup
  chrome.storage.local.get(['pluggyToken'], (result) => {
    updateUi(result.pluggyToken);
  });

  // Copia o token para a área de transferência ao clicar no ícone
  copyBtn.addEventListener('click', () => {
    if (activeToken) {
      navigator.clipboard.writeText(activeToken).then(() => {
        // Feedback visual no tom Rosa Brand do FinanceHub (#E05697)
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
              <rect width="14" height="14" x="8" y="8" rx="2" ry="2"/>
              <path d="M4 16c-1.1 0-2-.9-2-2V4c0-1.1.9-2 2-2h10c1.1 0 2 .9 2 2"/>
            </svg>
          `;
          copyBtn.style.backgroundColor = '';
          copyBtn.style.color = '';
        }, 1500);
      });
    }
  });
});
