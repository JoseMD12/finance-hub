import React, { useState, useEffect } from 'react';
import { Card } from '@/shared/components/Card/Card';
import { Button } from '@/shared/components/Button/Button';
import { KeyRound, ExternalLink, RefreshCw, CheckCircle2, AlertCircle, XCircle, Puzzle } from 'lucide-react';
import { CONNECTIONS_DEFAULTS } from '../constants/connectionsConstants';
import { formatDateTimeBR } from '@/shared/utils/formatters';
import { IconCircle } from '@/shared/components/IconCircle/IconCircle';
import { StatusBadge } from '@/shared/components/StatusBadge/StatusBadge';
import type { PluggySyncSummaryDto } from '../types/connections.types';

interface PluggySyncPanelProps {
  token: string;
  isConnected: boolean;
  isSyncing: boolean;
  lastSync: PluggySyncSummaryDto | null;
  onSync: (token: string) => void;
  onSaveToken: (token: string) => void;
  onClearToken: () => void;
}

export const PluggySyncPanel: React.FC<PluggySyncPanelProps> = ({
  token,
  isConnected,
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

  const hasToken = Boolean(token.trim());

  return (
    <Card className="flex flex-col gap-3.5 border-slate-200 py-4 px-4 md:px-5">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div className="flex items-center gap-2.5">
          <IconCircle icon={KeyRound} tone="brand" size="sm" />
          <div className="flex items-center gap-2">
            <h2 className="text-xs font-bold text-slate-800">
              Sessão Meu.Pluggy
            </h2>
            {isConnected ? (
              <StatusBadge icon={CheckCircle2} tone="success" className="px-2 py-0.5 text-[10px]">
                Conectado
              </StatusBadge>
            ) : (
              <StatusBadge icon={AlertCircle} tone="warning" className="px-2 py-0.5 text-[10px]">
                Pendente
              </StatusBadge>
            )}
          </div>
        </div>

        <div className="flex flex-col items-end gap-1.5 self-end sm:self-auto">
          {lastSync && (
            <span className="text-[11px] text-slate-400 font-medium whitespace-nowrap">
              Atualizado: {formatDateTimeBR(lastSync.syncedAtUtc)}
            </span>
          )}
          <div className="flex items-center gap-1.5">
            <Button
              variant="ghost"
              size="sm"
              onClick={() => window.open(CONNECTIONS_DEFAULTS.EXTENSION_DOCS_URL, '_blank', 'noopener,noreferrer')}
              className="text-[11px] h-7 px-2.5"
              aria-label="Download da extensão Chrome FinanceHub Sync"
            >
              <Puzzle className="w-3 h-3" />
              Extensão Chrome
            </Button>

            <Button
              variant="ghost"
              size="sm"
              onClick={() => window.open(CONNECTIONS_DEFAULTS.PLUGGY_PORTAL_URL, '_blank', 'noopener,noreferrer')}
              className="text-[11px] h-7 px-2.5"
              aria-label="Abrir portal Meu.Pluggy em nova aba"
            >
              <ExternalLink className="w-3 h-3" />
              Abrir Meu.Pluggy
            </Button>
          </div>
        </div>
      </div>

      <div className="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3">
        <div className="relative flex items-center w-full max-w-sm">
          <input
            id="pluggy-access-token-input"
            type="password"
            value={inputToken}
            onChange={handleInputChange}
            placeholder="Cole o token da extensão..."
            className="w-full pr-8 pl-3 py-1.5 border border-border-subtle rounded-xl text-xs focus:outline-none form-input-focus bg-surface-card"
          />
          {hasToken && (
            <button
              type="button"
              onClick={onClearToken}
              title="Limpar token"
              aria-label="Remover token"
              className="absolute right-2.5 text-slate-400 hover:text-status-danger transition-colors"
            >
              <XCircle className="w-3.5 h-3.5" />
            </button>
          )}
        </div>

        <Button
          variant="primary"
          size="sm"
          onClick={handleTriggerSync}
          isLoading={isSyncing}
          disabled={!inputToken.trim()}
          className="btn-primary-glow text-xs h-8 whitespace-nowrap self-end sm:self-auto"
        >
          {!isSyncing && <RefreshCw className="w-3.5 h-3.5" />}
          Importar Dados Meu.Pluggy
        </Button>
      </div>
    </Card>
  );
};
