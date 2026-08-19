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
    const normalized = payload.replace(/-/g, '+').replace(/_/g, '/');
    const padded = normalized.padEnd(Math.ceil(normalized.length / 4) * 4, '=');
    const claims = JSON.parse(atob(padded)) as Record<string, unknown>;
    const email = String(claims.email || claims['https://api.pluggy.ai/email'] || 'Sessão identificada');
    const name = String(claims.name || claims.nameid || claims['https://api.pluggy.ai/name'] || email);
    return { name, email };
  } catch {
    return { name: 'Sessão identificada', email: 'Token encontrado' };
  }
}
