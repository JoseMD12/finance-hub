export const CONNECTIONS_STORAGE_KEYS = {
  ACCESS_TOKEN: 'pluggy_access_token',
  LAST_SYNC: 'pluggy_last_sync_summary',
} as const;

export const CONNECTIONS_DEFAULTS = {
  STALE_TIME_MS: 120000,
  DEFAULT_BADGE: 'Meu.Pluggy Open Finance',
  OFFLINE_ACCEPTED_FORMATS: '.ofx, .csv, .pdf',
  PLUGGY_PORTAL_URL: 'https://meu.pluggy.ai',
  EXTENSION_DOCS_URL: 'https://chromewebstore.google.com',
} as const;

export const INSTITUTION_LOGO_URLS = {
  itau: 'https://upload.wikimedia.org/wikipedia/commons/1/19/Ita%C3%BA_Unibanco_logo_2023.svg',
  inter: 'https://upload.wikimedia.org/wikipedia/commons/8/8f/Logo_do_banco_Inter_%282023%29.svg',
  mercadoPago: 'https://cdn.simpleicons.org/mercadopago/00AEEF',
} as const;

const INSTITUTION_LOGO_MATCHES: ReadonlyArray<readonly [string, string]> = [
  ['itau', INSTITUTION_LOGO_URLS.itau],
  ['inter', INSTITUTION_LOGO_URLS.inter],
  ['mercadopago', INSTITUTION_LOGO_URLS.mercadoPago],
];

export function getInstitutionLogoUrl(institutionName: string): string | null {
  const normalizedName = institutionName
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .replace(/[^a-z0-9]/g, '');

  return INSTITUTION_LOGO_MATCHES.find(([match]) => normalizedName.includes(match))?.[1] ?? null;
}
