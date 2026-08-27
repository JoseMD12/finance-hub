import { useEffect } from 'react';

const SCROLLING_CLASS = 'is-scrolling';
const IDLE_DELAY_MS = 120;

/**
 * Desliga o hit-testing de ponteiro enquanto a página rola.
 *
 * Durante o scroll o browser reavalia `:hover` a cada frame, e cada elemento que
 * passa sob o cursor entra e sai do estado de hover. Em listas longas isso vira
 * repaint por linha por frame — a causa clássica de travamento e flicker no scroll.
 *
 * Suspender `pointer-events` durante o gesto elimina a classe inteira do problema,
 * inclusive para componentes futuros. O hover volta ~120ms após o último evento de
 * scroll, o que é imperceptível na prática.
 *
 * Deve ser chamado uma única vez, no shell da aplicação.
 */
export function useScrollPointerLock(): void {
  useEffect(() => {
    let idleTimer: ReturnType<typeof setTimeout> | undefined;
    let isLocked = false;

    const unlock = () => {
      isLocked = false;
      document.body.classList.remove(SCROLLING_CLASS);
    };

    const handleScroll = () => {
      if (!isLocked) {
        isLocked = true;
        document.body.classList.add(SCROLLING_CLASS);
      }
      clearTimeout(idleTimer);
      idleTimer = setTimeout(unlock, IDLE_DELAY_MS);
    };

    // `capture` para também pegar o scroll de containers aninhados (tabela, sidebar).
    window.addEventListener('scroll', handleScroll, { passive: true, capture: true });

    return () => {
      window.removeEventListener('scroll', handleScroll, { capture: true });
      clearTimeout(idleTimer);
      unlock();
    };
  }, []);
}
