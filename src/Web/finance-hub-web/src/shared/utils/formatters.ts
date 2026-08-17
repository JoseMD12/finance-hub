// Instâncias singleton para evitar alocação excessiva em listas de transações
const brlFormatter = new Intl.NumberFormat('pt-BR', {
  style: 'currency',
  currency: 'BRL',
});

const dateFormatter = new Intl.DateTimeFormat('pt-BR');

/**
 * Formata um valor numérico para moeda brasileira (BRL).
 * Exemplo: 1234.56 -> "R$ 1.234,56"
 */
export function formatCurrencyBRL(value: number | string | null | undefined): string {
  if (value === null || value === undefined || Number.isNaN(Number(value))) {
    return 'R$ 0,00';
  }
  return brlFormatter.format(Number(value));
}

/**
 * Formata uma data ISO ou Date para DD/MM/YYYY.
 */
export function formatDateBR(dateString: string | Date | null | undefined): string {
  if (!dateString) return '-';
  const date = typeof dateString === 'string' ? new Date(dateString) : dateString;
  return Number.isNaN(date.getTime()) ? '-' : dateFormatter.format(date);
}

/**
 * Mascara CPF de acordo com a LGPD (ex: "12345678900" -> "***.456.789-**").
 */
export function maskSensitiveCpf(cpf: string | null | undefined): string {
  if (!cpf) return '***.***.***-**';
  const clean = cpf.replace(/\D/g, '');
  if (clean.length !== 11) return '***.***.***-**';
  return `***.${clean.substring(3, 6)}.${clean.substring(6, 9)}-**`;
}

/**
 * Mascara conta bancária (ex: "12345-6" -> "***45-6").
 */
export function maskSensitiveAccount(account: string | null | undefined): string {
  if (!account) return '****-*';
  const clean = account.trim();
  if (clean.length <= 4) return '****';
  return `***${clean.slice(-4)}`;
}

/**
 * Mascara chave PIX (detecta e-mail ou telefone/CPF).
 */
export function maskSensitivePixKey(key: string | null | undefined): string {
  if (!key) return '***';
  if (key.includes('@')) {
    const [user, domain] = key.split('@');
    return `${user.slice(0, 2)}***@${domain}`;
  }
  const clean = key.replace(/\D/g, '');
  if (clean.length === 11) return maskSensitiveCpf(clean);
  return `${key.slice(0, 3)}*****${key.slice(-2)}`;
}
