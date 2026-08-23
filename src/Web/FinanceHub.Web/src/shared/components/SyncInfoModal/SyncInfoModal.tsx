import React from 'react';
import { Modal } from '@/shared/components/Modal/Modal';
import { Clock, CreditCard, ArrowRightLeft, ShieldCheck } from 'lucide-react';

export interface SyncInfoModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export const SyncInfoModal: React.FC<SyncInfoModalProps> = ({ isOpen, onClose }) => {
  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Como funciona a sincronização bancária"
    >
      <div className="flex flex-col gap-4 text-xs text-slate-600">
        <p className="text-slate-700 leading-relaxed">
          O FinanceHub conecta-se aos seus bancos através do Open Finance regulado pelo Banco Central. 
          O tempo de disponibilização dos lançamentos varia de acordo com o tipo de operação:
        </p>

        <div className="grid grid-cols-1 gap-3">
          {/* Pix e Contas */}
          <div className="p-3.5 rounded-xl bg-surface-ground border border-border-subtle flex items-start gap-3">
            <div className="w-8 h-8 rounded-lg bg-emerald-50 text-emerald-600 ring-1 ring-emerald-500/20 flex items-center justify-center shrink-0 mt-0.5">
              <ArrowRightLeft className="w-4 h-4" aria-hidden="true" />
            </div>
            <div className="flex flex-col gap-0.5">
              <span className="font-bold text-slate-800 text-xs">
                Saldos em Conta e Pix
              </span>
              <span className="text-[11px] text-emerald-700 font-semibold">
                Atualização em poucos minutos
              </span>
              <p className="text-[11px] text-slate-500 mt-1 leading-normal">
                Transferências via Pix, TED e alterações no saldo da conta corrente são refletidas quase imediatamente no Open Finance.
              </p>
            </div>
          </div>

          {/* Cartões de Crédito */}
          <div className="p-3.5 rounded-xl bg-surface-ground border border-border-subtle flex items-start gap-3">
            <div className="w-8 h-8 rounded-lg bg-blue-50 text-blue-600 ring-1 ring-blue-500/20 flex items-center justify-center shrink-0 mt-0.5">
              <CreditCard className="w-4 h-4" aria-hidden="true" />
            </div>
            <div className="flex flex-col gap-0.5">
              <span className="font-bold text-slate-800 text-xs">
                Compras no Cartão de Crédito
              </span>
              <span className="text-[11px] text-blue-700 font-semibold">
                Prazo de compensação: 24h a 48h (D+1 ou D+2)
              </span>
              <p className="text-[11px] text-slate-500 mt-1 leading-normal">
                Ao passar o cartão, a compra reduz o limite no app do banco como &ldquo;Autorização Pendente&rdquo;. As APIs do Open Finance só liberam a transação após a liquidação contábil da bandeira (Visa/Mastercard) junto ao emissor.
              </p>
            </div>
          </div>

          {/* Segurança e Integridade */}
          <div className="p-3.5 rounded-xl bg-surface-ground border border-border-subtle flex items-start gap-3">
            <div className="w-8 h-8 rounded-lg bg-brand-light text-brand ring-1 ring-brand/20 flex items-center justify-center shrink-0 mt-0.5">
              <ShieldCheck className="w-4 h-4" aria-hidden="true" />
            </div>
            <div className="flex flex-col gap-0.5">
              <span className="font-bold text-slate-800 text-xs">
                Garantia de Integridade
              </span>
              <p className="text-[11px] text-slate-500 mt-1 leading-normal">
                O FinanceHub não estima nem duplica compras pendentes. Todos os dados são auditados e validados contra os registros oficiais dos bancos.
              </p>
            </div>
          </div>
        </div>

        <div className="flex items-center justify-between pt-2 border-t border-border-subtle text-[11px] text-slate-400">
          <span className="inline-flex items-center gap-1">
            <Clock className="w-3.5 h-3.5 text-slate-400" aria-hidden="true" />
            Sincronização sob demanda disponível na aba Conexões
          </span>
        </div>
      </div>
    </Modal>
  );
};
