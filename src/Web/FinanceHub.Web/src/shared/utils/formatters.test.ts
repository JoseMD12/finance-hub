import { describe, it, expect } from 'vitest';
import { formatCurrencyBRL, formatDateBR, maskSensitiveCpf, maskSensitiveAccount, maskSensitivePixKey } from './formatters';

describe('Formatters Utility', () => {
  describe('formatCurrencyBRL', () => {
    it('formats numbers into BRL currency string', () => {
      const result = formatCurrencyBRL(1234.56);
      expect(result).toContain('1.234,56');
    });

    it('returns R$ 0,00 for invalid inputs', () => {
      expect(formatCurrencyBRL(null)).toBe('R$ 0,00');
      expect(formatCurrencyBRL(undefined)).toBe('R$ 0,00');
      expect(formatCurrencyBRL('invalid')).toBe('R$ 0,00');
    });
  });

  describe('formatDateBR', () => {
    it('formats ISO dates to Brazilian format', () => {
      const formatted = formatDateBR('2026-08-17T00:00:00Z');
      expect(formatted).toBe('17/08/2026');
    });

    it('returns dash for null or empty dates', () => {
      expect(formatDateBR(null)).toBe('-');
      expect(formatDateBR('')).toBe('-');
    });
  });

  describe('LGPD Masking Utilities', () => {
    it('masks CPF correctly', () => {
      expect(maskSensitiveCpf('12345678900')).toBe('***.456.789-**');
    });

    it('masks bank accounts correctly', () => {
      expect(maskSensitiveAccount('12345-6')).toBe('***45-6');
    });

    it('masks email PIX keys correctly', () => {
      expect(maskSensitivePixKey('usuario@email.com')).toBe('us***@email.com');
    });
  });
});
