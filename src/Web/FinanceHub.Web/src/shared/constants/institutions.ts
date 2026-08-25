export const INSTITUTION_LOGO_URLS = {
  itau: 'https://upload.wikimedia.org/wikipedia/commons/1/19/Ita%C3%BA_Unibanco_logo_2023.svg',
  inter: 'https://upload.wikimedia.org/wikipedia/commons/8/8f/Logo_do_banco_Inter_%282023%29.svg',
  mercadoPago: 'https://cdn.simpleicons.org/mercadopago/00AEEF',
  nubank: 'https://cdn.simpleicons.org/nubank/820AD1',
} as const;

export interface InstitutionInfo {
  readonly id: string;
  readonly name: string;
  readonly code: string;
  readonly logoUrl: string;
  readonly tagClass: string;
}

const INSTITUTION_CONFIGS: ReadonlyArray<InstitutionInfo & { readonly matches: readonly string[] }> = [
  {
    id: 'itau',
    name: 'Itaú Unibanco',
    code: 'ITAÚ',
    logoUrl: INSTITUTION_LOGO_URLS.itau,
    tagClass: 'bg-amber-50 text-amber-900 border-amber-200/80',
    matches: ['itau', 'itauunibanco'],
  },
  {
    id: 'inter',
    name: 'Banco Inter',
    code: 'INTER',
    logoUrl: INSTITUTION_LOGO_URLS.inter,
    tagClass: 'bg-orange-50 text-orange-900 border-orange-200/80',
    matches: ['inter', 'bancointer'],
  },
  {
    id: 'mercadopago',
    name: 'Mercado Pago',
    code: 'MERCADO PAGO',
    logoUrl: INSTITUTION_LOGO_URLS.mercadoPago,
    tagClass: 'bg-sky-50 text-sky-900 border-sky-200/80',
    matches: ['mercadopago', 'mp', 'mercado'],
  },
  {
    id: 'nubank',
    name: 'Nubank',
    code: 'NUBANK',
    logoUrl: INSTITUTION_LOGO_URLS.nubank,
    tagClass: 'bg-purple-50 text-purple-900 border-purple-200/80',
    matches: ['nubank', 'nu'],
  },
];

export function normalizeInstitutionKey(input: string): string {
  return input
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .replace(/[^a-z0-9]/g, '');
}

export function getInstitutionInfo(institutionNameOrId: string): InstitutionInfo {
  const normalized = normalizeInstitutionKey(institutionNameOrId);
  const found = INSTITUTION_CONFIGS.find((config) =>
    config.matches.some((match) => normalized.includes(match))
  );

  if (found) {
    return {
      id: found.id,
      name: found.name,
      code: found.code,
      logoUrl: found.logoUrl,
      tagClass: found.tagClass,
    };
  }

  return {
    id: normalized || 'unknown',
    name: institutionNameOrId.toUpperCase(),
    code: institutionNameOrId.toUpperCase(),
    logoUrl: '',
    tagClass: 'bg-surface-ground text-slate-700 border-border-subtle',
  };
}

export function getInstitutionLogoUrl(institutionNameOrId: string): string | null {
  const info = getInstitutionInfo(institutionNameOrId);
  return info.logoUrl || null;
}
