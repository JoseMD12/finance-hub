# FinanceHub — Frontend Web Architecture & UI Specification (Phase 6)

> **Status**: `Ready for Implementation`  
> **Last Updated**: `2026-08-15`  
> **Target Module**: `src/Web/finance-hub-web`  
> **Target Branch**: `feature/frontend-web-dashboard`  
> **Stack**: `React 19 / 18 + Vite + TypeScript + Tailwind CSS (v4/v3 + Tokens) + TanStack Query v5 + React Router + Lucide Icons + Recharts + Sonner + React Hook Form + Zod`

---

## 🏛️ 1. Topologia de Diretórios & Inserção no Repositório

O projeto frontend é inserido no repositório mantendo o isolamento de microsserviços e bibliotecas compartilhadas:

```text
./
├── .agents/                      # Harness de IA, Rules, Knowledge e Skills
├── src/
│   ├── Services/                 # Microsserviços .NET 10 (Clean Architecture + DDD)
│   │   ├── ApiGateway/           # BFF Entrypoint (Porta 5000)
│   │   ├── AuthConsent/          # FAPI / OAuth2 Consent Manager
│   │   ├── ItauIntegration/      # Connector Itaú
│   │   ├── MercadoPagoIntegration/# Connector Mercado Pago
│   │   ├── InterIntegration/     # Connector Banco Inter
│   │   └── TransactionAggregator/# Canonical Ledger & Deduplication
│   ├── Shared/                   # Bibliotecas transversais .NET 10
│   │   ├── FinanceHub.Shared.Certificates/
│   │   ├── FinanceHub.Shared.Messaging/
│   │   └── FinanceHub.Shared.Observability/
│   └── Web/                      # Camada de Apresentação Web (Phase 6)
│       └── finance-hub-web/      # Single-Page Application (React + Vite)
└── tests/                        # Testes de Serviços Backend e E2E
```

### 📁 Estrutura Interna da SPA (`src/Web/finance-hub-web/`)
```text
src/Web/finance-hub-web/
├── public/                       # Favicon, assets estáticos puros
├── src/
│   ├── app/                      # Bootstrap, providers, layout e roteamento com Lazy Loading
│   │   ├── layout/               # AppLayout (Sidebar, Topbar, MainContainer)
│   │   ├── providers/            # QueryClientProvider, Toaster, RouterProvider
│   │   └── routes/               # AppRoutes.tsx (com React.lazy e Suspense)
│   ├── assets/                   # Imagens e vetores estáticos
│   ├── features/                 # Módulos verticais de negócio isolados (Slices)
│   │   ├── auth/                 # Login, sessão em memória e interceptores
│   │   ├── dashboard/            # Saldo consolidado, métricas, gráficos agregados
│   │   ├── transactions/         # Extrato, categorização, conciliação e parcelas
│   │   └── consents/             # Gestão de conexões Open Finance (Itaú, Inter, MP)
│   └── shared/                   # Recursos reutilizáveis e Design System
│       ├── api/                  # httpClient.ts (Axios + Silent Refresh Queue + RFC 7807)
│       ├── components/           # Componentes atômicos/compostos reutilizáveis
│       │   ├── Button/           # Botões Primário (Brand), Secundário (Teal), Outline, Ghost
│       │   ├── Select/           # Custom Select estilizado com menu elevado e teclado
│       │   ├── Modal/            # Diálogos acessíveis WAI-ARIA com focus trap e Escape
│       │   ├── Card/             # Superfícies elevadas com sombras e cantos arredondados
│       │   ├── Table/            # Tabela de dados financeiros com tipografia alinhada
│       │   └── Skeleton/         # Placeholders de carregamento estruturais
│       ├── hooks/                # useToast, useDebounce, useMediaQuery
│       ├── styles/               # index.css com tokens de cor e resets
│       ├── types/                # ProblemDetails, ApiError, DTOs compartilhados
│       └── utils/                # Singletons BRL/Date, cn(), mapeador de erros RFC 7807
├── package.json
├── tsconfig.json
├── tailwind.config.ts / postcss.config.js
└── vite.config.ts
```

---

## 🎨 2. Design System, Identidade Visual & Regras Rígidas de UI

Baseado nas referências visuais em `refs-projeto-controle/` (`Paleta.pdf`, `Orçamento.pdf`, `Calendário.pdf`, `Modal.pdf`, `Login.pdf`):

### 2.1 Paleta de Cores e Tokens Oficiais
| Token Tailwind / CSS | Hex | Uso Obrigatório |
| :--- | :--- | :--- |
| `brand` / `--color-brand-primary` | `#E05697` | Botões de ação primária, destaques ativos, tabs |
| `brand-dark` / `--color-brand-dark` | `#941B5C` | Hover em botões primários, títulos de destaque |
| `brand-light` / `--color-brand-light` | `#FCE7F3` | Fundo de badges e opções selecionadas |
| `secondary` / `--color-secondary-base` | `#1D555A` | Fundo da Sidebar, cabeçalhos de tabela |
| `secondary-dark` / `--color-secondary-dark` | `#164347` | Hover e acentos de sidebar |
| `secondary-light` / `--color-secondary-light`| `#E6F4F1` | Hover de opções no Custom Select |
| `tertiary` / `--color-tertiary-base` | `#FF7338` | Badges de atenção moderada, acentos gráficos |
| `surface-ground` / `--color-bg-app` | `#F4F7F6` | Fundo geral da aplicação |
| `surface-card` / `--color-surface-card` | `#FFFFFF` | Cards, modais e containers elevados |
| `status-success` | `#2ECC71` | Receitas (+), status de sincronização OK |
| `status-danger` | `#FF5964` | Despesas (-), alertas de erro, contas vencidas |
| `status-info` | `#38BDF8` | Informações, dicas, registros |
| `status-warning` | `#F59E0B` | Contas a vencer, avisos de teto de gastos |

### 2.2 Regras Rígidas de UI & Acessibilidade
1. **Política Estrita de Zero Emojis**: Proibido o uso de emojis em qualquer elemento (títulos, botões, modais, toasts ou tabelas). Indicadores visuais devem usar ícones vetoriais outline limpos (`lucide-react` / SVG com `stroke: currentColor`).
2. **Controles de Formulário Componentizados (Custom Select)**: Proibido o uso de `<select>` nativo sem estilização. Todos os selects devem utilizar o componente `shared/components/Select` com menu flutuante elevado, hover estilizado e suporte a teclado.
3. **Formatação BRL e LGPD Singletons**: Uso mandatório de `formatCurrencyBRL` e `formatDateBR` via singletons de `Intl.NumberFormat`, com mascaramento para CPF (`maskSensitiveCpf`), Contas Bancárias (`maskSensitiveAccount`) e Chaves PIX (`maskSensitivePixKey`).

---

## 🔒 3. Arquitetura de Comunicação HTTP, Segurança & RFC 7807

### 3.1 Segurança de Sessão e Resiliência contra Concorrência 401
- **Armazenamento de Tokens**: *Access Token* armazenado estritamente em memória JS (`authStore`).
- **Fila de Silent Refresh (*Retry Queue*)**: O interceptor Axios em `shared/api/httpClient.ts` intercepta retornos 401 simultâneos, enfileira as requisições pendentes, dispara apenas uma chamada para `/api/v1/auth/refresh` e reexecuta todas as requisições pendentes após o refresh, prevenindo múltiplos logouts.

### 3.2 Normalização de Erros RFC 7807 (`ProblemDetails`) & Formulários
- Toda resposta de erro da API retorna `ProblemDetails` (`title`, `status`, `detail`, `errorCode`, `traceId`, `errors`).
- **Mapeamento Campo a Campo**: A função `mapProblemDetailsToFormErrors` converte erros do FluentValidation do .NET 10 (PascalCase) diretamente para o `setError` do `react-hook-form` (camelCase).
- **Notificações**: Utilitário `showApiError` notifica via `Sonner` com código de erro amigável.

---

## ⚡ 4. Gerenciamento de Estado & Server State (TanStack Query v5)

1. **Separação Rígida de Estado**:
   - Server State: 100% gerenciado via TanStack Query com **Query Keys Factory** (`features/<feature>/api/<feature>Keys.ts`).
   - UI State: React Contexts leves ou `useState` local.
2. **Políticas de Cache por Volatilidade**:
   - Saldos Consolidados: `staleTime: 1 min` / `gcTime: 10 min`.
   - Extratos de Transações: `staleTime: 2 min` / `gcTime: 15 min`.
   - Consentimentos Bancários: `staleTime: 5 min` / `gcTime: 30 min`.
3. **Optimistic Updates**: Suporte a atualizações antecipadas de UI com rollback no `onError` e ressincronização no `onSettled`.

---

## 🧭 5. Escopo Inicial de Páginas & Rotas (Fase 6 MVP)

| Rota | Feature | Descrição & Componentes |
| :--- | :--- | :--- |
| `/login` | `features/auth` | Tela de autenticação baseada em `Login.pdf` com formulário tipado (`react-hook-form` + `zod`) e integração Bearer JWT. |
| `/` ou `/dashboard` | `features/dashboard` | Visão consolidada multi-bancos: cards de saldo (Itaú, Inter, Mercado Pago), resumo de receitas/despesas, gráfico Donut de categorias e feed de transações. |
| `/transacoes` | `features/transactions` | Extrato financeiro unificado: tabela rica com status visual, filtros por período/categoria/banco, busca, parcelamento (`2/5`) e modal de detalhes. |
| `/conexoes` | `features/consents` | Gestão de consentimentos Open Finance: status de conexões bancárias ativas/expiradas, renovação de tokens e conexão de novas contas. |

---

## 🚀 6. Checklist de Execução & Próximos Passos

- [x] **Preparação do Harness de IA**: Regras, knowledge e skill adicionadas em `.agents/`.
- [x] **Homologação e Auditoria**: Auditorias técnicas concluídas com notas **9.98** e **9.80**.
- [x] **Isolamento de Branch**: Branch dedicada `feature/frontend-web-dashboard` criada e ativa.
- [ ] **Passo 1 — Scaffolding do Projeto Vite**:
  - Inicializar `src/Web/finance-hub-web` com Vite + React + TypeScript.
  - Instalar dependências core: `react-router-dom`, `@tanstack/react-query`, `axios`, `lucide-react`, `recharts`, `sonner`, `clsx`, `tailwind-merge`, `tailwindcss`, `react-hook-form`, `@hookform/resolvers`, `zod`, `date-fns`.
- [ ] **Passo 2 — Configuração de Design Tokens & Tailwind**:
  - Configurar `tailwind.config.ts` e `index.css` com as variáveis da paleta do Figma.
- [ ] **Passo 3 — Primitivas Compartilhadas (`src/shared/`)**:
  - Implementar `httpClient.ts`, `Button`, `Select`, `Card`, `Modal`, `Table` e utilitários BRL.
- [ ] **Passo 4 — Layout Shell & Roteamento (`src/app/`)**:
  - Criar `Sidebar`, `Topbar`, `AppLayout` e configurar `AppRoutes.tsx` com `React.lazy()`.
- [ ] **Passo 5 — Implementação das Features**:
  - Scaffold e construção de `features/auth`, `features/dashboard`, `features/transactions` e `features/consents`.
- [ ] **Passo 6 — Testes & Validação E2E**:
  - Testes com Vitest e React Testing Library, validação de build limpo (`npm run build`).
