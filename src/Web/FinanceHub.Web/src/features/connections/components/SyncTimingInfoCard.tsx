import React, { useState } from 'react';
import { Card } from '@/shared/components/Card/Card';
import { Clock, ChevronDown, ChevronUp, ArrowRightLeft, CreditCard, ShieldCheck } from 'lucide-react';

export const SyncTimingInfoCard: React.FC = () => {
  const [isExpanded, setIsExpanded] = useState(false);

  return (
    <Card className="p-4 bg-surface-card border border-border-subtle shadow-card" hoverable={false}>
      <div className="flex items-center justify-between gap-3">
        <div className="flex items-center gap-2.5">
          <div className="w-8 h-8 rounded-xl bg-blue-50 text-blue-600 ring-1 ring-blue-500/20 flex items-center justify-center shrink-0">
            <Clock className="w-4 h-4" aria-hidden="true" />
          </div>
          <div className="flex flex-col">
            <span className="text-xs font-bold text-slate-800">
              Prazos de Sincronização Bancária
            </span>
            <span className="text-[11px] text-slate-500">
              Entenda como os bancos processam lançamentos de Pix e Cartão de Crédito
            </span>
          </div>
        </div>

        <button
          type="button"
          onClick={() => setIsExpanded(!isExpanded)}
          aria-expanded={isExpanded}
          className="inline-flex items-center gap-1 px-2.5 py-1 rounded-lg text-xs font-semibold text-slate-600 hover:text-brand hover:bg-brand-light transition-colors cursor-pointer"
        >
          <span>{isExpanded ? 'Ocultar detalhes' : 'Ver prazos'}</span>
          {isExpanded ? (
            <ChevronUp className="w-3.5 h-3.5" aria-hidden="true" />
          ) : (
            <ChevronDown className="w-3.5 h-3.5" aria-hidden="true" />
          )}
        </button>
      </div>

      {isExpanded && (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-3 pt-3.5 mt-3 border-t border-border-subtle/80 text-xs animate-in fade-in slide-in-from-top-1 duration-150">
          <div className="p-3 rounded-xl bg-surface-ground border border-border-subtle flex flex-col gap-1">
            <span className="font-semibold text-slate-800 flex items-center gap-1.5">
              <ArrowRightLeft className="w-3.5 h-3.5 text-emerald-600" aria-hidden="true" />
              Pix e Contas Correntes
            </span>
            <span className="text-[11px] text-emerald-700 font-semibold">
              Disponível em poucos minutos
            </span>
            <p className="text-[11px] text-slate-500 mt-0.5 leading-relaxed">
              Transferências Pix e alterações de saldo em conta são integradas de forma quase imediata nas consultas Open Finance.
            </p>
          </div>

          <div className="p-3 rounded-xl bg-surface-ground border border-border-subtle flex flex-col gap-1">
            <span className="font-semibold text-slate-800 flex items-center gap-1.5">
              <CreditCard className="w-3.5 h-3.5 text-blue-600" aria-hidden="true" />
              Cartão de Crédito (D+1 / D+2)
            </span>
            <span className="text-[11px] text-blue-700 font-semibold">
              Compensação em 24h a 48h
            </span>
            <p className="text-[11px] text-slate-500 mt-0.5 leading-relaxed">
              Compras recentes ficam retidas como &ldquo;Autorização Pendente&rdquo; no banco e são liberadas no feed Open Finance após a liquidação pela bandeira.
            </p>
          </div>

          <div className="p-3 rounded-xl bg-surface-ground border border-border-subtle flex flex-col gap-1">
            <span className="font-semibold text-slate-800 flex items-center gap-1.5">
              <ShieldCheck className="w-3.5 h-3.5 text-brand" aria-hidden="true" />
              Consistência dos Dados
            </span>
            <span className="text-[11px] text-brand-dark font-semibold">
              Sem lançamentos duplicados
            </span>
            <p className="text-[11px] text-slate-500 mt-0.5 leading-relaxed">
              O FinanceHub aguarda o fechamento oficial do banco para evitar duplicidades e garantir precisão contábil.
            </p>
          </div>
        </div>
      )}
    </Card>
  );
};
