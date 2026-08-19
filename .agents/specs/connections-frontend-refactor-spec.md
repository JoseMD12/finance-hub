# Especificação Técnica: Refatoração da Tela de Conexões (Frontend)

**Documento:** `.agents/specs/connections-frontend-refactor-spec.md`  
**Status:** 🟢 Finalizada / Aprovada para Implementação  
**Data:** 18/08/2026  

---

## 1. 🎯 Objetivo & Visão Geral

Refatorar a feature de **Conexões** (`src/features/connections/`) no frontend React 19 + Vite do **FinanceHub**, eliminando os dados mockados de instituições (Itaú, Inter, Mercado Pago estáticos) e adequando à arquitetura real com o conector Meu.Pluggy e importador off-line.

---

## 2. 🎨 Layout & Hierarquia Visual

A tela adotará uma **Página Única Unificada e Fluida** estruturada em seções verticais com hierarquia visual clara:

1. **Header da Página**: Título "Conexões", subtítulo explicativo e indicador de status global de conectividade.
2. **Seção 1: Conexão Meu.Pluggy Open Finance**:
   - **PluggySyncPanel**:
     - *Sem Token / Expirado*: Instruções de captura via extensão Chrome FinanceHub Sync, link externo para `meu.pluggy.ai`, input de `accessToken` e botão "Sincronizar".
     - *Com Token Ativo*: Badge de conexão ativa, data/hora da última sincronização, botão de re-sincronizar e ação de desconectar/substituir token.
   - **SyncSummaryBanner**: Exibido após sincronização bem-sucedida, resumindo bancos, contas e transações ingeridas.
   - **Grade de Instituições / Contas Reais**:
     - Lista dinâmica alimentada pelos dados reais retornados pelo backend (`useConnectedAccountsQuery` / `DashboardSummaryDto`).
     - Exibe cards com nome da instituição, número da conta formatado, saldo e data da última atualização.
     - **EmptyConnectionsState**: Exibido quando não há token ou quando nenhuma conta foi sincronizada ainda.
3. **Seção 2: Importador de Extratos Off-line**:
   - **FileImporterCard**: Exibido em modo Preview com badge **"Em Breve"**, informando que o suporte a arquivos OFX, CSV e PDF está em desenvolvimento no microsserviço `FinanceHub.FileImporter`.

---

## 3. 🚦 Estados da Tela e Fluxo de Dados

1. **Estado Desconectado (Sem Token / Expirado)**:
   - Painel do Pluggy destaca a captura do token via extensão Chrome.
   - Grade de instituições exibe `EmptyConnectionsState` ("Nenhuma instituição conectada").
   - Importador off-line em modo preview "Em Breve".

2. **Estado Conectado (Token Válido & Sincronizado)**:
   - Exibe as instituições bancárias reais que retornaram do backend.
   - Botão de re-sincronização dispara `useSyncPluggyMutation`, que:
     - Envia o cabeçalho `X-Pluggy-Access-Token`.
     - Invalida os caches de `connectionKeys.all`, `['dashboard']` e `['transactions']`.
     - Exibe toast de sucesso com resumo da sincronização.

---

## 4. 🛡️ Tratamento de Erros e RFC 7807

| Cenário de Erro | Código HTTP | ErrorCode | Comportamento no Frontend |
| :--- | :--- | :--- | :--- |
| Token não informado / vazio | 400 | `NULL_OR_EMPTY_PLUGGY_ACCESS_TOKEN` | Toast de alerta via `showApiError` |
| Sessão expirada no Meu.Pluggy | 401 | `UNAUTHORIZED` / `INVALID_TOKEN` | Toast de erro orientando a reabrir a extensão e obter novo token |
| Falha de comunicação com Gateway | 502 | `BAD_GATEWAY` | Toast de erro com mensagem amigável |

---

## 5. 🏛️ Estrutura de Diretórios Alvo (Vertical Slice)

```text
src/features/connections/
├── api/
│   ├── connectionsApi.ts          # Chamadas HTTP tipadas (BFF Gateway)
│   └── connectionKeys.ts         # Query Key Factory fortemente tipada
├── components/
│   ├── ConnectionCard.tsx        # Card de instituição/conta conectada (dados reais)
│   ├── EmptyConnectionsState.tsx # Estado vazio quando não há instituições conectadas
│   ├── PluggySyncPanel.tsx       # Card de inserção/gerenciamento do token de sessão Meu.Pluggy
│   ├── SyncSummaryBanner.tsx     # Feedback visual das transações/contas ingeridas no último sync
│   └── FileImporterCard.tsx      # Card de importação off-line com badge 'Em Breve'
├── hooks/
│   ├── useSyncPluggyMutation.ts  # TanStack Mutation para acionar sincronização
│   ├── usePluggyToken.ts         # Gerenciamento reativo do token (sessionStorage/state)
│   └── useConnectedAccountsQuery.ts # Query para listar contas e instituições ativas
├── types/
│   └── connections.types.ts      # DTOs e contratos de resposta
└── pages/
    └── ConnectionsPage.tsx       # Página orquestradora
```

---

## 6. 💅 Conformidade com Design System & Regras Arquiteturais

- **Zero Emojis**: Apenas ícones vetoriais outline (`lucide-react`).
- **Off-white Standard**: Superfícies usam `bg-surface-card` / `#FAFCFB`, sem uso de `#FFFFFF` puro.
- **Formatação Brasileira**: Moedas com `formatCurrencyBRL` e datas com `formatDateBR`.
- **Sem títulos com '&'**: Nome direto "Conexões".
- **Sem barras verticais (`|`)**: Títulos limpos com tipografia e espaçamento do design system.
