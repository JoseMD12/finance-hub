import { ArrowLeft } from 'lucide-react';

interface FinanceHubButtonProps {
  readonly onClick: () => void;
}

export function FinanceHubButton({ onClick }: Readonly<FinanceHubButtonProps>) {
  return (
    <button type="button" className="financehub-button" onClick={onClick}>
      <ArrowLeft aria-hidden="true" />
      Voltar para o FinanceHub
    </button>
  );
}
