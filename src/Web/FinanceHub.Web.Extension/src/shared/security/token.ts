export function isJwtShape(value: unknown): value is string {
  return typeof value === 'string'
    && value.trim().length > 30
    && value.split('.').length === 3;
}

export interface DisplayIdentity {
  readonly name: string;
  readonly email: string;
}

export function decodeDisplayIdentity(token: string): DisplayIdentity {
  try {
    const payload = token.split('.')[1];
    if (!payload) throw new Error('Missing token payload');
    const normalized = payload.replaceAll('-', '+').replaceAll('_', '/');
    const padded = normalized.padEnd(Math.ceil(normalized.length / 4) * 4, '=');
    const claims = JSON.parse(atob(padded)) as Record<string, unknown>;
    const rawEmail = claims.email ?? claims['https://api.pluggy.ai/email'];
    const email = typeof rawEmail === 'string' ? rawEmail : 'Sessão identificada';
    const rawName = claims.name ?? claims.nameid ?? claims['https://api.pluggy.ai/name'];
    const name = typeof rawName === 'string' ? rawName : email;
    return { name, email };
  } catch {
    return { name: 'Sessão identificada', email: 'Token encontrado' };
  }
}
