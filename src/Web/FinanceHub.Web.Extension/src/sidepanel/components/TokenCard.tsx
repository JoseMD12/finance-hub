import { Check, Copy } from 'lucide-react';

interface TokenCardProps {
  hasToken: boolean;
  onCopy: () => void;
  copied: boolean;
}

export function TokenCard({ hasToken, onCopy, copied }: TokenCardProps) {
  return (
    <section className={`token-card${hasToken ? ' token-card-found' : ''}`} aria-live="polite">
      <div className="token-card-content">
        <div>
          <div className="token-title-wrapper">
            {hasToken && <Check className="token-check" aria-hidden="true" />}
            <strong>{hasToken ? 'Token encontrado' : 'Nenhum token'}</strong>
          </div>
          <p>{hasToken ? 'Copie o token para usar no FinanceHub.' : 'Nenhuma sessão disponível.'}</p>
        </div>
        <button
          type="button"
          className="icon-button"
          onClick={onCopy}
          disabled={!hasToken}
          aria-label={copied ? 'Token copiado' : 'Copiar token'}
          title={copied ? 'Token copiado' : 'Copiar token'}
        >
          {copied ? <Check aria-hidden="true" /> : <Copy aria-hidden="true" />}
        </button>
      </div>
    </section>
  );
}
