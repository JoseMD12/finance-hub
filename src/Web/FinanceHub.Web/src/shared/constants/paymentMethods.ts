export const PAYMENT_METHOD_MAP: Record<string, string> = {
  pix: 'Pix',
  credit: 'Crédito',
  credito: 'Crédito',
  creditcard: 'Crédito',
  debit: 'Débito',
  debito: 'Débito',
  debitcard: 'Débito',
  ted: 'TED',
  doc: 'DOC',
  banktransfer: 'Transferência',
  other: 'Outro',
};

export function parsePaymentMethodName(channel?: string | null): string {
  if (!channel) return PAYMENT_METHOD_MAP.other;

  const key = channel
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .replace(/[^a-z0-9]/g, '');

  return PAYMENT_METHOD_MAP[key] ?? PAYMENT_METHOD_MAP.other;
}
