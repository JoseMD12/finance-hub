import { getAccessToken } from './authStorage';

/**
 * Identificação do usuário logado, derivada das claims do token.
 *
 * Existe porque a `Sidebar` exibia nome e e-mail de uma pessoa específica escritos no JSX —
 * o que não escala para outro usuário e coloca dado pessoal no repositório.
 */
export interface SessionUser {
  readonly name: string;
  readonly email?: string;
}

const FALLBACK_NAME = 'Usuário';

interface JwtClaims {
  readonly name?: string;
  readonly email?: string;
  readonly preferred_username?: string;
  readonly sub?: string;
}

/** Decodifica o payload do JWT. Não valida assinatura — quem valida é o backend. */
function decodeClaims(token: string): JwtClaims | null {
  try {
    const payload = token.split('.')[1];
    if (!payload) return null;

    const normalized = payload.replace(/-/g, '+').replace(/_/g, '/');
    const decoded = decodeURIComponent(
      atob(normalized)
        .split('')
        .map((char) => `%${`00${char.charCodeAt(0).toString(16)}`.slice(-2)}`)
        .join(''),
    );

    return JSON.parse(decoded) as JwtClaims;
  } catch {
    // Token malformado não pode derrubar o layout: cai no rótulo neutro.
    return null;
  }
}

export function getSessionUser(): SessionUser {
  const token = getAccessToken();
  if (!token) return { name: FALLBACK_NAME };

  const claims = decodeClaims(token);
  if (!claims) return { name: FALLBACK_NAME };

  const email = claims.email;
  const name = claims.name ?? claims.preferred_username ?? email?.split('@')[0] ?? claims.sub;

  return { name: name || FALLBACK_NAME, email };
}
