import { defineConfig } from 'wxt';
import { loadEnv } from 'vite';
import path from 'node:path';

// Load .env from repository root (/home/josemd12/Code/FinanceHub) and extension folder
const repositoryRoot = path.resolve(__dirname, '../../../');
const env = {
  ...process.env,
  ...loadEnv('development', repositoryRoot, ''),
  ...loadEnv('production', repositoryRoot, ''),
  ...loadEnv('development', __dirname, ''),
  ...loadEnv('production', __dirname, ''),
};

const financeHubWebUrl =
  env.FINANCEHUB_WEB_URL ||
  env.VITE_FINANCEHUB_WEB_URL ||
  'http://localhost:3000';

const financeHubApiUrl =
  env.FINANCEHUB_API_URL ||
  env.VITE_API_GATEWAY_URL ||
  env.API_GATEWAY_URL ||
  'http://localhost:5050';

function toOriginPattern(url: string): string {
  return `${new URL(url).origin}/*`;
}

export default defineConfig({
  srcDir: 'src',
  modules: ['@wxt-dev/module-react'],
  vite: () => ({
    envDir: repositoryRoot,
    define: {
      __FINANCEHUB_WEB_URL__: JSON.stringify(financeHubWebUrl),
      __FINANCEHUB_API_URL__: JSON.stringify(financeHubApiUrl),
    },
  }),
  manifest: {
    name: 'FinanceHub — Pluggy Token Sync',
    version: '1.0.0',
    description: 'Captura o token do Meu.Pluggy e exibe as contas conectadas no FinanceHub.',
    permissions: ['storage', 'webRequest', 'sidePanel', 'scripting', 'tabs'],
    host_permissions: [
      'https://meu.pluggy.ai/*',
      'https://my-api.pluggy.ai/*',
      toOriginPattern(financeHubWebUrl),
      toOriginPattern(financeHubApiUrl),
    ],
    action: {
      default_title: 'FinanceHub Pluggy Sync',
    },
    side_panel: {
      default_path: 'sidepanel.html',
    },
  },
});
