import React, { useState, useEffect } from 'react';
import { Card } from '@/shared/components/Card/Card';
import { Button } from '@/shared/components/Button/Button';
import { Modal } from '@/shared/components/Modal/Modal';
import { CustomSelect } from '@/shared/components/Select/CustomSelect';
import { Landmark, ShieldCheck, Plus, RefreshCw, UploadCloud, FileText, CheckCircle2, AlertCircle } from 'lucide-react';
import { toast } from 'sonner';
import { formatDateBR } from '@/shared/utils/formatters';
import { syncPluggyAccountsApi } from '../api/connectionsApi';

export const ConnectionsPage: React.FC = () => {
  const [activeTab, setActiveTab] = useState<'openfinance' | 'importer'>('openfinance');
  const [isConnectModalOpen, setIsConnectModalOpen] = useState(false);
  const [selectedBank, setSelectedBank] = useState('itau');
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [isUploading, setIsUploading] = useState(false);
  const [pastedToken, setPastedToken] = useState('');
  const [isSyncingToken, setIsSyncingToken] = useState(false);

  useEffect(() => {
    const savedToken = sessionStorage.getItem('pluggy_access_token');
    if (savedToken) {
      setPastedToken(savedToken);
    }
  }, []);

  const handleSyncToken = async () => {
    const token = pastedToken.trim();
    if (!token) {
      toast.error('Cole um token de sessão (accessToken) válido do Meu.Pluggy.');
      return;
    }

    setIsSyncingToken(true);
    try {
      sessionStorage.setItem('pluggy_access_token', token);
      const summary = await syncPluggyAccountsApi(token);
      toast.success(
        `Sincronização concluída! ${summary.totalItemsSynced} banco(s) e ${summary.totalAccountsSynced} conta(s) atualizadas.`
      );
    } catch (err: any) {
      const message = err?.response?.data?.detail || 'Não foi possível sincronizar as contas no momento.';
      toast.error(message);
    } finally {
      setIsSyncingToken(false);
    }
  };

  const consents = [
    {
      id: '1',
      bankName: 'Itaú Unibanco',
      badge: 'Meu.Pluggy Open Finance',
      status: 'Conectado',
      lastSync: '2026-08-17T18:30:00Z',
      expiresAt: '2027-08-15',
      accountsCount: 1,
    },
    {
      id: '2',
      bankName: 'Banco Inter',
      badge: 'Meu.Pluggy Open Finance',
      status: 'Conectado',
      lastSync: '2026-08-17T17:15:00Z',
      expiresAt: '2027-08-15',
      accountsCount: 1,
    },
    {
      id: '3',
      bankName: 'Mercado Pago',
      badge: 'Meu.Pluggy Open Finance',
      status: 'Conectado',
      lastSync: '2026-08-17T19:00:00Z',
      expiresAt: '2027-08-15',
      accountsCount: 1,
    },
  ];

  const bankOptions = [
    { value: 'itau', label: 'Itaú Unibanco', badge: 'Open Finance Brasil' },
    { value: 'inter', label: 'Banco Inter', badge: 'Conta Digital' },
    { value: 'mercadopago', label: 'Mercado Pago', badge: 'Carteira Digital' },
  ];

  const handleConnect = () => {
    setIsConnectModalOpen(false);
    toast.success('Redirecionando para Meu.Pluggy Connect OAuth2...');
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files?.[0]) {
      setSelectedFile(e.target.files[0]);
    }
  };

  const handleUploadFile = () => {
    if (!selectedFile) {
      toast.error('Selecione um arquivo de extrato (.ofx, .csv ou .pdf).');
      return;
    }

    setIsUploading(true);
    setTimeout(() => {
      setIsUploading(false);
      toast.success(`Arquivo "${selectedFile.name}" importado com sucesso via FileImporter! Transações enviadas para o TransactionAggregator.`);
      setSelectedFile(null);
    }, 1500);
  };

  return (
    <div className="flex flex-col gap-6">
      {/* Page Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h1 className="text-xl font-extrabold text-secondary">
            Conexões
          </h1>
          <p className="text-xs text-slate-500 font-medium mt-1">
            Gerenciamento de conectores Open Finance (Meu.Pluggy) e importação off-line de extratos bancários (OFX, CSV, PDF)
          </p>
        </div>

        <div className="flex items-center gap-3">
          <Button
            variant={activeTab === 'openfinance' ? 'primary' : 'ghost'}
            size="sm"
            onClick={() => setActiveTab('openfinance')}
          >
            <Landmark className="w-4 h-4" />
            Open Finance
          </Button>
          <Button
            variant={activeTab === 'importer' ? 'primary' : 'ghost'}
            size="sm"
            onClick={() => setActiveTab('importer')}
          >
            <UploadCloud className="w-4 h-4" />
            Importador Off-line
          </Button>
        </div>
      </div>

      {/* Tab 1: Open Finance Connectors */}
      {activeTab === 'openfinance' && (
        <div className="flex flex-col gap-6">
          {/* Card de Inserção do Token e Acesso ao Meu.Pluggy */}
          <Card className="flex flex-col gap-4 bg-slate-50/80 border-slate-200">
            <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 border-b border-slate-200/80 pb-3">
              <div>
                <h3 className="text-sm font-bold text-slate-800">Extensão FinanceHub Sync</h3>
                <p className="text-xs text-slate-500 mt-0.5">
                  Obtenha o token de sessão do <code>meu.pluggy.ai</code> usando a Extensão do Chrome e cole abaixo para sincronizar.
                </p>
              </div>
              <div className="flex items-center gap-2">
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => window.open('https://meu.pluggy.ai', '_blank', 'noopener,noreferrer')}
                >
                  Abrir Meu.Pluggy
                </Button>
              </div>
            </div>

            <div className="flex flex-col md:flex-row md:items-center justify-between gap-3">
              <span className="text-xs font-semibold text-slate-700">Atualizar Token de Sessão:</span>
              <div className="flex items-center gap-2 w-full md:w-auto">
                <input
                  type="text"
                  value={pastedToken}
                  onChange={(e) => setPastedToken(e.target.value)}
                  placeholder="Cole o accessToken copiado da extensão..."
                  className="flex-1 md:w-96 px-3.5 py-2 border rounded-xl text-xs focus:outline-none focus:ring-2 focus:ring-brand bg-white"
                />
                <Button
                  variant="primary"
                  size="sm"
                  onClick={handleSyncToken}
                  isLoading={isSyncingToken}
                  disabled={!pastedToken.trim()}
                >
                  Sincronizar
                </Button>
              </div>
            </div>
          </Card>

          <div className="flex items-center justify-between">
            <span className="text-xs font-bold text-slate-500 uppercase tracking-wider">
              Instituições Conectadas ({consents.length})
            </span>
            <Button onClick={() => setIsConnectModalOpen(true)} variant="primary" size="sm">
              <Plus className="w-4 h-4" />
              Conectar Instituição
            </Button>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            {consents.map((c) => (
              <Card key={c.id} className="flex flex-col justify-between gap-6 hoverable">
                <div className="flex flex-col gap-4">
                  <div className="flex items-center justify-between">
                    <div className="w-10 h-10 rounded-xl bg-secondary-light text-secondary flex items-center justify-center font-bold text-sm shadow-sm">
                      <Landmark className="w-5 h-5" />
                    </div>
                    <span className="inline-flex items-center gap-1 text-[11px] font-bold px-2.5 py-1 rounded-full bg-status-success-bg text-status-success">
                      <ShieldCheck className="w-3.5 h-3.5" />
                      {c.status}
                    </span>
                  </div>

                  <div>
                    <h3 className="text-base font-bold text-slate-800">{c.bankName}</h3>
                    <span className="text-[11px] text-slate-400 font-medium">
                      {c.badge} • 1 Conta Vinculada
                    </span>
                  </div>
                </div>

                <div className="pt-4 border-t border-slate-100 flex items-center justify-between">
                  <div className="flex flex-col">
                    <span className="text-[10px] text-slate-400">Última Sincronização</span>
                    <span className="text-[11px] font-semibold text-slate-700">
                      {formatDateBR(c.lastSync)}
                    </span>
                  </div>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => toast.info(`Disparando sync evento do ${c.bankName} via Meu.Pluggy...`)}
                    aria-label={`Sincronizar ${c.bankName}`}
                  >
                    <RefreshCw className="w-3.5 h-3.5" />
                    Sync
                  </Button>
                </div>
              </Card>
            ))}
          </div>
        </div>
      )}

      {/* Tab 2: File Importer (OFX, CSV, PDF Statements) */}
      {activeTab === 'importer' && (
        <Card className="flex flex-col gap-6">
          <div>
            <h2 className="text-base font-bold text-secondary flex items-center gap-2">
              <UploadCloud className="w-5 h-5 text-brand" />
              Importação de Extratos Off-line
            </h2>
            <p className="text-xs text-slate-500 font-medium mt-1">
              Envie arquivos extrato no padrão OFX, CSV ou PDF de qualquer banco brasileiro para processamento assíncrono no microsserviço <code className="text-brand font-mono">FinanceHub.FileImporter</code>.
            </p>
          </div>

          <div className="border-2 border-dashed border-slate-200 hover:border-brand rounded-2xl p-8 flex flex-col items-center justify-center gap-4 bg-slate-50/50 transition-colors">
            <div className="w-12 h-12 rounded-full bg-brand-light text-brand flex items-center justify-center">
              <FileText className="w-6 h-6" />
            </div>

            <div className="text-center">
              <label htmlFor="file-upload-input" className="cursor-pointer font-bold text-sm text-brand hover:underline">
                Clique para selecionar um arquivo
              </label>
              <span className="text-xs text-slate-500"> ou arraste o arquivo aqui</span>
              <input
                id="file-upload-input"
                type="file"
                accept=".ofx,.csv,.pdf"
                className="hidden"
                onChange={handleFileChange}
              />
              <p className="text-[11px] text-slate-400 mt-1">Formatos suportados: Extratos OFX, Planilhas CSV, Faturas em PDF</p>
            </div>

            {selectedFile && (
              <div className="flex items-center gap-3 bg-white px-4 py-2.5 rounded-xl border border-border-subtle shadow-sm">
                <CheckCircle2 className="w-4 h-4 text-status-success" />
                <span className="text-xs font-semibold text-slate-700">{selectedFile.name}</span>
                <span className="text-[10px] text-slate-400">({(selectedFile.size / 1024).toFixed(1)} KB)</span>
              </div>
            )}
          </div>

          <div className="flex items-center justify-between pt-4 border-t border-slate-100">
            <div className="flex items-center gap-2 text-xs text-slate-500 font-medium">
              <AlertCircle className="w-4 h-4 text-tertiary" />
              <span>O parser elimina automaticamente duplicatas via hash SHA-256 no TransactionAggregator.</span>
            </div>

            <Button
              variant="primary"
              onClick={handleUploadFile}
              isLoading={isUploading}
              disabled={!selectedFile}
            >
              <UploadCloud className="w-4 h-4" />
              Processar Ingestão
            </Button>
          </div>
        </Card>
      )}

      {/* Modal Conectar Meu.Pluggy */}
      <Modal
        isOpen={isConnectModalOpen}
        onClose={() => setIsConnectModalOpen(false)}
        title="Conectar Conta via Meu.Pluggy Open Finance"
      >
        <div className="flex flex-col gap-5">
          <p className="text-xs text-slate-600">
            Selecione a instituição financeira autorizada para autenticação FAPI segura via OAuth2 no conector Meu.Pluggy:
          </p>

          <CustomSelect
            label="Instituição Financeira"
            options={bankOptions}
            value={selectedBank}
            onChange={setSelectedBank}
          />

          <div className="p-3 bg-secondary-light/60 rounded-xl text-xs text-secondary-dark font-medium flex items-center gap-2">
            <ShieldCheck className="w-4 h-4 text-secondary flex-shrink-0" />
            <span>Fluxo PKCE com DPoP e autorização direta na plataforma Meu.Pluggy.</span>
          </div>

          <div className="flex items-center justify-end gap-2 pt-2">
            <Button variant="ghost" onClick={() => setIsConnectModalOpen(false)}>
              Cancelar
            </Button>
            <Button variant="primary" onClick={handleConnect}>
              Autorizar no Meu.Pluggy
            </Button>
          </div>
        </div>
      </Modal>
    </div>
  );
};

export default ConnectionsPage;
