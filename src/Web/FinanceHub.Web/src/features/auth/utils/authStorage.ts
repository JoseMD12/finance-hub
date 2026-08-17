// Armazenamento em memória para Access Token (mitigação contra XSS)
let memoryAccessToken: string | null = null;

export function getAccessToken(): string | null {
  return memoryAccessToken;
}

export function setAccessToken(token: string | null): void {
  memoryAccessToken = token;
}

export function clearSession(): void {
  memoryAccessToken = null;
}
