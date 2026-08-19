import { describe, expect, it } from 'vitest';
import { decodeDisplayIdentity, isJwtShape } from './token';

describe('token security helpers', () => {
  it('accepts only JWT-shaped values', () => {
    expect(isJwtShape('header.payload-that-is-long-enough.signature-value')).toBe(true);
    expect(isJwtShape('not-a-token')).toBe(false);
    expect(isJwtShape(null)).toBe(false);
  });

  it('decodes display-only identity claims without exposing the token', () => {
    const payload = btoa(JSON.stringify({ email: 'user@example.com', name: 'Finance User' }));
    const token = `header.${payload}.signature`;

    expect(decodeDisplayIdentity(token)).toEqual({
      email: 'user@example.com',
      name: 'Finance User',
    });
  });

  it('returns a safe fallback for malformed claims', () => {
    expect(decodeDisplayIdentity('header.invalid.signature')).toEqual({
      email: 'Token encontrado',
      name: 'Sessão identificada',
    });
  });
});
