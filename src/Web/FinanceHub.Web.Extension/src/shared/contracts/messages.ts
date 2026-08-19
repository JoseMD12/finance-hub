export const MESSAGE_TYPES = {
  tokenCaptured: 'financehub/token-captured',
  logoutDetected: 'financehub/logout-detected',
} as const;

export interface TokenCapturedMessage {
  readonly type: typeof MESSAGE_TYPES.tokenCaptured;
  readonly token: string;
}

export interface LogoutDetectedMessage {
  readonly type: typeof MESSAGE_TYPES.logoutDetected;
}

export type RuntimeMessage = TokenCapturedMessage | LogoutDetectedMessage;

export function isRuntimeMessage(value: unknown): value is RuntimeMessage {
  if (typeof value !== 'object' || value === null || !('type' in value)) return false;

  const message = value as { type?: unknown; token?: unknown };
  if (message.type === MESSAGE_TYPES.logoutDetected) return true;
  return message.type === MESSAGE_TYPES.tokenCaptured
    && typeof message.token === 'string'
    && message.token.length > 30;
}
