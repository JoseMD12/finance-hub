import React, { useState } from 'react';
import { Card } from '@/shared/components/Card/Card';
import { Button } from '@/shared/components/Button/Button';
import { Modal } from '@/shared/components/Modal/Modal';
import { CustomSelect } from '@/shared/components/Select/CustomSelect';
import { Landmark, ShieldCheck, Plus, RefreshCw } from 'lucide-react';
import { toast } from 'sonner';

export const ConsentsPage: React.FC = () => {
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [selectedBank, setSelectedBank] = useState('itau');

  const consents = [
    {
      id: '1',
      bankName: 'Itaú Unibanco',
      badge: 'Open Finance',
      status: 'Ativo',
      expiresAt: '2027-08-15',
      accountsCount: 1,
    },
    {
      id: '2',
      bankName: 'Banco Inter',
      badge: 'Conta Digital',
      status: 'Ativo',
      expiresAt: '2027-08-15',
      accountsCount: 1,
    },
    {
      id: '3',
      bankName: 'Mercado Pago',
      badge: 'Carteira',
      status: 'Ativo',
      expiresAt: '2027-08-15',
      accountsCount: 1,
    },
  ];

  const bankOptions = [
    { value: 'itau', label: 'Itaú Unibanco', badge: 'Open Finance' },
    { value: 'inter', label: 'Banco Inter', badge: 'Conta Digital' },
    { value: 'mercadopago', label: 'Mercado Pago', badge: 'Carteira' },
  ];

  const handleConnect = () => {
    setIsModalOpen(false);
    toast.success('Iniciando redirecionamento para autorização Open Finance...');
  };

  return (
    <div className="flex flex-col gap-6">
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h1 className="text-xl font-extrabold text-secondary">Conexões Open Finance</h1>
          <p className="text-xs text-slate-500 font-medium">Gerenciamento de consentimentos FAPI e renovação de tokens</p>
        </div>
        <Button onClick={() => setIsModalOpen(true)} variant="primary">
          <Plus className="w-4 h-4" />
          Conectar Nova Instituição
        </Button>
      </div>

      {/* Consents List */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        {consents.map((c) => (
          <Card key={c.id} className="flex flex-col justify-between gap-6">
            <div className="flex flex-col gap-4">
              <div className="flex items-center justify-between">
                <div className="w-10 h-10 rounded-xl bg-secondary/10 text-secondary flex items-center justify-center font-bold text-sm">
                  <Landmark className="w-5 h-5" />
                </div>
                <span className="inline-flex items-center gap-1 text-[11px] font-bold px-2.5 py-1 rounded-full bg-status-success-bg text-status-success">
                  <ShieldCheck className="w-3.5 h-3.5" />
                  {c.status}
                </span>
              </div>

              <div>
                <h3 className="text-base font-bold text-slate-800">{c.bankName}</h3>
                <span className="text-[11px] text-slate-400 font-medium">{c.badge} • 1 Conta Vinculada</span>
              </div>
            </div>

            <div className="pt-4 border-t border-slate-100 flex items-center justify-between">
              <span className="text-[11px] text-slate-400">Expira em: {c.expiresAt}</span>
              <Button variant="ghost" size="sm" onClick={() => toast.info(`Sincronizando ${c.bankName}...`)}>
                <RefreshCw className="w-3.5 h-3.5" />
                Sincronizar
              </Button>
            </div>
          </Card>
        ))}
      </div>

      {/* Modal para Conectar Banco */}
      <Modal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        title="Conectar Instituição Bancária"
      >
        <div className="flex flex-col gap-5">
          <p className="text-xs text-slate-600">
            Selecione a instituição financeira autorizada pelo Banco Central para iniciar o compartilhamento de dados via Open Finance Brasil:
          </p>

          <CustomSelect
            label="Instituição Bancária"
            options={bankOptions}
            value={selectedBank}
            onChange={setSelectedBank}
          />

          <div className="p-3 bg-secondary-light/60 rounded-xl text-xs text-secondary-dark font-medium flex items-center gap-2">
            <ShieldCheck className="w-4 h-4 text-secondary flex-shrink-0" />
            <span>Conexão criptografada de ponta a ponta com certificados ICP-Brasil (mTLS).</span>
          </div>

          <div className="flex items-center justify-end gap-2 pt-2">
            <Button variant="ghost" onClick={() => setIsModalOpen(false)}>
              Cancelar
            </Button>
            <Button variant="primary" onClick={handleConnect}>
              Autorizar no Banco
            </Button>
          </div>
        </div>
      </Modal>
    </div>
  );
};

export default ConsentsPage;
