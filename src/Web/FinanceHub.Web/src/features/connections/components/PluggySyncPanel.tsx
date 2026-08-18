import React, { useState, useEffect } from 'react';
import { Card } from '@/shared/components/Card/Card';
import { Button } from '@/shared/components/Button/Button';
import { KeyRound, ExternalLink, RefreshCw, CheckCircle2, AlertCircle, XCircle } from 'lucide-react';
import { CONNECTIONS_DEFAULTS } from '../constants/connectionsConstants';
import { formatDateBR } from '@/shared/utils/formatters';
import type { PluggySyncSummaryDto } from '../types/connections.types';

interface PluggySyncPanelProps {
  token: string;
  isSyncing: boolean;
  lastSync: PluggySyncSummaryDto | null;
  onSync: (token: string) => void;
  onSaveToken: (token: string) => void;
  onClearToken: () => void;
}

export const PluggySyncPanel: React.FC<PluggySyncPanelProps> = ({
  token,
  isSyncing,
  lastSync,
  onSync,
  onSaveToken,
  onClearToken,
}) => {
  const [inputToken, setInputToken] = useState(token);

  useEffect(() => {
    setInputToken(token);
  }, [token]);

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const val = e.target.value;
    setInputToken(val);
    onSaveToken(val);
  };

  const handleTriggerSync = () => {
    if (!inputToken.trim()) return;
    onSync(inputToken.trim());
  };

  const hasActiveSession = Boolean(token.trim());

  return (
    <Card className="flex flex-col gap-4 border-slate-200">
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 border-b border-slate-200/80 pb-3">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-xl bg-brand-light text-brand flex items-center justify-center font-bold text-sm shadow-sm flex-shrink-0">
            <KeyRound className="w-5 h-5" />
          </div>
          <div>
            <h2 className="text-sm font-bold text-slate-800">
              Sessão Open Finance Meu.Pluggy
            </h2>
            <p className="text-xs text-slate-500 mt-0.5">
              Sincronização via token de acesso da Extensão FinanceHub Sync para Google Chrome.
            </p>
          </div>
        </div>

        <div className="flex items-center gap-2">
          <Button
            variant="ghost"
            size="sm"
            onClick={() => window.open(CONNECTIONS_DEFAULTS.PLUGGY_PORTAL_URL, '_blank', 'noopener,noreferrer')}
            aria-label="Abrir portal Meu.Pluggy em nova aba"
          >
            <ExternalLink className="w-3.5 h-3.5" />
            Abrir Meu.Pluggy
          </Button>
        </div>
      </div>

      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div className="flex flex-col gap-1 flex-1">
          <label htmlFor="pluggy-access-token-input" className="text-xs font-semibold text-slate-700 flex items-center gap-1.5">
            <span>Token de Sessão (accessToken):</span>
            {hasActiveSession ? (
              <span className="inline-flex items-center gap-1 text-[11px] font-bold px-2 py-0.5 rounded-full bg-status-success-bg text-status-success">
                <CheckCircle2 className="w-3 h-3" />
                Token Configurado
              </span>
            ) : (
              <span className="inline-flex items-center gap-1 text-[11px] font-bold px-2 py-0.5 rounded-full bg-status-warning-bg text-status-warning">
                <AlertCircle className="w-3 h-3" />
                Aguardando Token
              </span>
            )}
          </label>
          <div className="flex items-center gap-2 w-full">
            <input
              id="pluggy-access-token-input"
              type="password"
              value={inputToken}
              onChange={handleInputChange}
              placeholder="Cole aqui o accessToken copiado da extensão..."
              className="flex-1 px-3.5 py-2 border border-border-subtle rounded-xl text-xs focus:outline-none form-input-focus bg-surface-card"
            />
            {hasActiveSession && (
              <Button
                variant="ghost"
                size="sm"
                onClick={onClearToken}
                title="Limpar token de sessão"
                aria-label="Remover token de sessão atual"
              >
                <XCircle className="w-4 h-4 text-slate-400 hover:text-status-danger" />
              </Button>
            )}
          </div>
        </div>

        <div className="flex flex-col sm:flex-row items-stretch sm:items-center gap-2">
          <Button
            variant="primary"
            size="md"
            onClick={handleTriggerSync}
            isLoading={isSyncing}
            disabled={!inputToken.trim()}
            className="btn-primary-glow"
          >
            <RefreshCw className={`w-4 h-4 ${isSyncing ? 'animate-spin' : ''}`} />
            Sincronizar Contas
          </Button>
        </div>
      </div>

      {lastSync && (
        <div className="flex flex-wrap items-center justify-between gap-2 pt-2 border-t border-slate-100 text-[11px] text-slate-500">
          <span>Última sincronização registrada: <strong className="text-slate-700 font-semibold">{formatDateBR(lastSync.syncedAtUtc)}</strong></span>
          <span className="text-slate-400">Status da sessão: Ativa</span>
        </div>
      )}
    </Card>
  );
};
