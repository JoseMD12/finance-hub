export interface PluggyAccount {
  readonly itemId?: string;
  readonly institutionName?: string;
  readonly name?: string;
  readonly type?: string;
  readonly subtype?: string;
  readonly balance?: number;
  readonly creditData?: {
    readonly availableCreditLimit?: number;
    readonly level?: string;
    readonly totalCreditLimit?: number;
  };
}

export function isPluggyAccount(value: unknown): value is PluggyAccount {
  return typeof value === 'object' && value !== null;
}

export function isPluggyAccountList(value: unknown): value is PluggyAccount[] {
  return Array.isArray(value) && value.every(isPluggyAccount);
}
