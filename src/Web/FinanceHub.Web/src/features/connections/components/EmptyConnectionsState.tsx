import React from 'react';
import { Card } from '@/shared/components/Card/Card';
import { Landmark, ShieldAlert } from 'lucide-react';
import { IconCircle } from '@/shared/components/IconCircle/IconCircle';

interface EmptyConnectionsStateProps {
  hasToken: boolean;
}

export const EmptyConnectionsState: React.FC<EmptyConnectionsStateProps> = ({ hasToken }) => {
  return (
    <Card className="flex flex-col items-center justify-center p-8 text-center border-dashed border-slate-200">
      <IconCircle
        icon={hasToken ? Landmark : ShieldAlert}
        tone={hasToken ? 'secondary' : 'muted'}
        size="lg"
        className="mb-3 h-12 w-12 rounded-2xl"
      />
      <h3 className="text-sm font-bold text-slate-800 mb-1">
        {hasToken ? 'Nenhuma conta sincronizada' : 'Nenhuma instituição conectada'}
      </h3>
      <p className="text-xs text-slate-500 max-w-md">
        {hasToken
          ? 'Clique em "Importar Dados Meu.Pluggy" no painel acima para carregar suas instituições bancárias.'
          : 'Insira o token de sessão da extensão FinanceHub Sync no campo acima e sincronize suas contas Open Finance.'}
      </p>
    </Card>
  );
};
