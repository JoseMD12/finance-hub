import { ArrowLeft } from 'lucide-react';

interface FinanceHubButtonProps {
  onClick: () => void;
}

export function FinanceHubButton({ onClick }: FinanceHubButtonProps) {
  return (
    <button type="button" className="financehub-button" onClick={onClick}>
      <ArrowLeft aria-hidden="true" />
      Voltar para o FinanceHub
    </button>
  );
}
