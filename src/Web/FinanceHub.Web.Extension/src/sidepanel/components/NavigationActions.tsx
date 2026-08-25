import { ArrowLeft, ExternalLink } from 'lucide-react';

interface NavigationActionsProps {
  readonly onOpenMeuPluggy: () => void;
  readonly onOpenFinanceHub: () => void;
  readonly isOnPluggySite: boolean;
  readonly isOnFinanceHubSite: boolean;
}

export function NavigationActions({
  onOpenMeuPluggy,
  onOpenFinanceHub,
  isOnPluggySite,
  isOnFinanceHubSite,
}: Readonly<NavigationActionsProps>) {
  const showPluggyButton = !isOnPluggySite;
  const showFinanceHubButton = !isOnFinanceHubSite;

  if (!showPluggyButton && !showFinanceHubButton) {
    return null;
  }

  return (
    <div className="navigation-actions-container">
      {showPluggyButton && (
        <button
          type="button"
          className="action-button action-button-pluggy"
          onClick={onOpenMeuPluggy}
          aria-label="Acessar Meu.Pluggy"
        >
          <ExternalLink aria-hidden="true" />
          Acessar Meu.Pluggy
        </button>
      )}

      {showFinanceHubButton && (
        <button
          type="button"
          className="action-button action-button-financehub"
          onClick={onOpenFinanceHub}
          aria-label="Voltar para o FinanceHub"
        >
          <ArrowLeft aria-hidden="true" />
          Voltar para o FinanceHub
        </button>
      )}
    </div>
  );
}
