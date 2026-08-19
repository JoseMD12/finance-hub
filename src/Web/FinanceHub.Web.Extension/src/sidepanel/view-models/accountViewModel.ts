import type { PluggyAccount } from '@financehub/web-shared';

export function getAccountType(account: PluggyAccount): string {
  const subtype = String(account.subtype || '').toUpperCase();
  const type = String(account.type || '').toUpperCase();

  if (subtype.includes('CREDIT') || type === 'CREDIT') return 'Crédito';
  if (subtype.includes('CHECKING')) return 'Conta corrente';
  if (subtype.includes('SAVINGS')) return 'Poupança';
  if (subtype.includes('INVEST')) return 'Investimentos';
  return account.name || 'Conta';
}

export function getAccountInstitution(account: PluggyAccount): string {
  return account.institutionName || 'Instituição';
}

export function getAccountName(account: PluggyAccount): string {
  return account.name || 'Conta conectada';
}
