import type { PluggyAccount } from '@financehub/web-shared';
import { Landmark } from 'lucide-react';
import { getAccountInstitution, getAccountName, getAccountType } from '../view-models/accountViewModel';

interface AccountsSectionProps {
  accounts: PluggyAccount[];
  isLoading: boolean;
  hasError: boolean;
}

export function AccountsSection({ accounts, isLoading, hasError }: AccountsSectionProps) {
  return (
    <section className="accounts-section">
      <h2>Contas conectadas</h2>
      {isLoading && <div className="accounts-state">Consultando contas conectadas...</div>}
      {!isLoading && hasError && <div className="accounts-state">Não foi possível carregar as contas agora.</div>}
      {!isLoading && !hasError && accounts.length === 0 && (
        <div className="accounts-state">Nenhuma conta retornada pelo backend.</div>
      )}
      {!isLoading && !hasError && accounts.length > 0 && (
        <div className="accounts-list">
          {accounts.map((account, index) => (
            <article className="account-row" key={`${account.itemId || getAccountInstitution(account)}-${index}`}>
              <div className="account-main">
                <strong>{getAccountInstitution(account)}</strong>
                <span>{getAccountName(account)}</span>
              </div>
              <span className="account-type"><Landmark aria-hidden="true" />{getAccountType(account)}</span>
            </article>
          ))}
        </div>
      )}
    </section>
  );
}
