import React from 'react';
import { Card } from '@/shared/components/Card/Card';
import { Landmark, ShieldAlert } from 'lucide-react';

interface EmptyConnectionsStateProps {
  hasToken: boolean;
}

export const EmptyConnectionsState: React.FC<EmptyConnectionsStateProps> = ({ hasToken }) => {
  return (
    <Card className="flex flex-col items-center justify-center p-8 text-center border-dashed border-slate-200">
      <div className="w-12 h-12 rounded-2xl bg-secondary-light text-secondary flex items-center justify-center mb-3">
        {hasToken ? <Landmark className="w-6 h-6" /> : <ShieldAlert className="w-6 h-6 text-slate-400" />}
      </div>
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
