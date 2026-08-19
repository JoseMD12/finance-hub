import { describe, expect, it } from 'vitest';
import { getAccountType } from './accountViewModel';

describe('account view model', () => {
  it.each([
    [{ type: 'CREDIT' }, 'Crédito'],
    [{ subtype: 'CHECKING' }, 'Conta corrente'],
    [{ subtype: 'SAVINGS' }, 'Poupança'],
    [{ subtype: 'INVESTMENT' }, 'Investimentos'],
  ])('maps Pluggy account %# to a display label', (account, expected) => {
    expect(getAccountType(account)).toBe(expected);
  });
});
