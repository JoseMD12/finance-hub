import { describe, expect, it, vi } from 'vitest';

vi.stubGlobal('__FINANCEHUB_WEB_URL__', 'http://localhost:3000');
vi.stubGlobal('__FINANCEHUB_API_URL__', 'http://localhost:5050');

import {
  isFinanceHubHost,
  isMeuPluggyHost,
  isTrustedSite,
  RUNTIME_URLS,
} from './runtime';

describe('runtime constants and trusted host rules', () => {
  it('correctly maps Meu.Pluggy URL to overview page', () => {
    expect(RUNTIME_URLS.meuPluggy).toBe('https://meu.pluggy.ai/overview');
  });

  it('correctly maps FinanceHub Web URL to /conexoes', () => {
    expect(RUNTIME_URLS.financeHubWeb).toBe('http://localhost:3000/conexoes');
  });

  it.each([
    ['https://meu.pluggy.ai', true],
    ['https://meu.pluggy.ai/overview', true],
    ['https://meu.pluggy.ai/login', true],
    ['https://meu.pluggy.ai/signin', true],
    ['https://app.pluggy.ai', false],
    ['https://google.com', false],
    ['', false],
    [undefined, false],
  ])('isMeuPluggyHost(%s) returns %s', (url, expected) => {
    expect(isMeuPluggyHost(url)).toBe(expected);
  });

  it.each([
    ['http://localhost:3000', true],
    ['http://localhost:3000/conexoes', true],
    ['http://127.0.0.1:3000', true],
    ['http://localhost:5173', true],
    ['http://localhost:5173/conexoes', true],
    ['https://youtube.com', false],
    ['https://google.com', false],
    ['', false],
    [undefined, false],
  ])('isFinanceHubHost(%s) returns %s', (url, expected) => {
    expect(isFinanceHubHost(url)).toBe(expected);
  });

  it.each([
    ['https://meu.pluggy.ai', true],
    ['https://meu.pluggy.ai/overview', true],
    ['http://localhost:3000', true],
    ['http://localhost:3000/conexoes', true],
    ['https://google.com', false],
    ['https://youtube.com', false],
    ['https://github.com', false],
    ['', false],
    [undefined, false],
  ])('isTrustedSite(%s) returns %s', (url, expected) => {
    expect(isTrustedSite(url)).toBe(expected);
  });
});
