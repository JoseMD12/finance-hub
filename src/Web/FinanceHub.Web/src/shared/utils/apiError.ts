import { toast } from 'sonner';
import { ApiError } from '../types/api.types';

export function showApiError(error: unknown, fallbackMessage = 'Erro ao processar operação.') {
  if (error instanceof ApiError) {
    const { title, detail, errorCode } = error.problemDetails;
    toast.error(title || 'Falha na Operação', {
      description: detail || fallbackMessage,
      action: errorCode ? { label: `Código: ${errorCode}`, onClick: () => {} } : undefined,
    });
    return;
  }

  toast.error(fallbackMessage);
}
