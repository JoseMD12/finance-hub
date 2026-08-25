import { Check, Copy } from 'lucide-react';

interface TokenCardProps {
  readonly hasToken: boolean;
  readonly onCopy: () => void;
  readonly copied: boolean;
  readonly customHelpText?: string;
}

export function TokenCard({ hasToken, onCopy, copied, customHelpText }: Readonly<TokenCardProps>) {
  return (
    <section className={`token-card${hasToken ? ' token-card-found' : ''}`} aria-live="polite">
      <div className="token-card-content">
        <div>
          <div className="token-title-wrapper">
            {hasToken && <Check className="token-check" aria-hidden="true" />}
            <strong>{hasToken ? 'Token encontrado' : 'Nenhum token'}</strong>
          </div>
          <p>
            {customHelpText ??
              (hasToken ? 'Copie o token para usar no FinanceHub.' : 'Nenhuma sessão disponível.')}
          </p>
        </div>
        <button
          type="button"
          className="icon-button"
          onClick={onCopy}
          disabled={!hasToken}
          aria-label={copied ? 'Token copiado para a área de transferência' : 'Copiar token de acesso'}
        >
          {copied ? <Check aria-hidden="true" /> : <Copy aria-hidden="true" />}
        </button>
      </div>
    </section>
  );
}
