import { describe, it, expect } from 'vitest';
import {
  formatCurrencyBRL,
  formatDateBR,
  formatTimeBR,
  formatPaymentMethod,
  maskSensitiveCpf,
  maskSensitiveAccount,
  maskSensitivePixKey,
} from './formatters';

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

  describe('formatTimeBR', () => {
    it('formats ISO date timestamp to HH:MM in America/Sao_Paulo timezone', () => {
      const formatted = formatTimeBR('2026-08-17T13:30:00Z');
      expect(formatted).toMatch(/\d{2}:\d{2}/);
    });

    it('returns default placeholder for null or empty dates', () => {
      expect(formatTimeBR(null)).toBe('--:--');
      expect(formatTimeBR('')).toBe('--:--');
    });
  });

  describe('formatPaymentMethod', () => {
    it('translates payment channels to standardized Brazilian labels', () => {
      expect(formatPaymentMethod('Pix')).toBe('Pix');
      expect(formatPaymentMethod('pix')).toBe('Pix');
      expect(formatPaymentMethod('Credit')).toBe('Crédito');
      expect(formatPaymentMethod('CreditCard')).toBe('Crédito');
      expect(formatPaymentMethod('Debit')).toBe('Débito');
      expect(formatPaymentMethod('DebitCard')).toBe('Débito');
      expect(formatPaymentMethod('Boleto')).toBe('Outro');
      expect(formatPaymentMethod('Transferência')).toBe('Outro');
      expect(formatPaymentMethod(null)).toBe('Outro');
      expect(formatPaymentMethod('')).toBe('Outro');
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
