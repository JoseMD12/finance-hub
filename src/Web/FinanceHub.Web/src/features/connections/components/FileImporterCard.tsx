import React from 'react';
import { Card } from '@/shared/components/Card/Card';
import { UploadCloud, FileSpreadsheet, Clock } from 'lucide-react';
import { CONNECTIONS_DEFAULTS } from '../constants/connectionsConstants';
import { IconCircle } from '@/shared/components/IconCircle/IconCircle';
import { StatusBadge } from '@/shared/components/StatusBadge/StatusBadge';

export const FileImporterCard: React.FC = () => {
  return (
    <Card className="flex flex-col gap-4 border-slate-200">
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-3 border-b border-slate-200/80 pb-3">
        <div className="flex items-center gap-3">
          <IconCircle icon={UploadCloud} tone="tertiary" size="lg" />
          <div>
            <h2 className="text-sm font-bold text-slate-800">
              Importação de Extratos Off-line
            </h2>
            <p className="text-xs text-slate-500 mt-0.5">
              Ingestão de arquivos de extratos bancários ({CONNECTIONS_DEFAULTS.OFFLINE_ACCEPTED_FORMATS}) para processamento automático.
            </p>
          </div>
        </div>

        <div>
          <StatusBadge icon={Clock} tone="warning">Em Breve</StatusBadge>
        </div>
      </div>

      <div className="border-2 border-dashed border-slate-200/90 rounded-2xl p-6 flex flex-col items-center justify-center gap-3 bg-slate-50/40 text-center">
        <div className="w-10 h-10 rounded-full bg-slate-100 text-slate-400 flex items-center justify-center">
          <FileSpreadsheet className="w-5 h-5" />
        </div>
        <div className="max-w-md">
          <p className="text-xs font-semibold text-slate-700">
            O microsserviço de processamento de arquivos está em desenvolvimento.
          </p>
          <p className="text-[11px] text-slate-400 mt-1">
            Em breve você poderá arrastar e processar arquivos OFX, CSV e faturas em PDF de forma assíncrona com deduplicação SHA-256.
          </p>
        </div>
      </div>
    </Card>
  );
};
