# FinanceHub — Frontend Web Architecture & UI Specification (Phase 6)

> **Status**: `Drafting / In Specification`  
> **Last Updated**: `2026-08-13`  
> **Target Module**: `src/Web/FinanceHub.Web`  
> **Stack**: `React 19 / 18 + Vite + TypeScript + Vanilla CSS / Tailwind (to be defined) + TanStack Query + React Router + Lucide Icons + Recharts`

---

## 🎨 1. Design System & Visual Identity (Inspirado no Figma & Refs)

Com base nas referências visuais de `refs-projeto-controle/` (`Paleta.pdf`, `Orçamento.pdf`, `Calendário.pdf`, `Modal.pdf`, `Login.pdf`):

### 1.1 Paleta de Cores & Tokens
| Token | Cor / Hex Estimado | Aplicação Principal |
| :--- | :--- | :--- |
| `--color-brand` | `#E05697` / `#D43A82` | Ações principais, botões primários, destaques ativos |
| `--color-brand-dark` | `#941B5C` / `#B01869` | Hover states, títulos de destaque, barras de progresso |
| `--color-secondary` | `#1D555A` / `#164347` | Sidebar background / dark accents, cabeçalhos de tabela |
| `--color-tertiary` | `#FF7338` / `#FF8A3D` | Badges de atenção, alertas moderados, acentos visuais |
| `--color-bg-main` | `#F4F7F6` / `#F8F9FA` | Fundo geral da aplicação (clean, alto contraste) |
| `--color-surface` | `#FFFFFF` | Cards elevados, modais, dropdowns, containers de dados |
| `--color-text-primary` | `#1E293B` / `#212529` | Tipografia principal de alta legibilidade |
| `--color-text-muted` | `#64748B` / `#47595E` | Subtítulos, labels, datas, metadados |
| `--color-success` | `#2ECC71` / `#10B981` | Entradas financeiras, saldo positivo, status OK |
| `--color-danger` | `#FF5964` / `#EF4444` | Saídas financeiras, despesas, alertas de erro |
| `--color-info` | `#38BDF8` / `#85C7F2` | Informações de transações, dicas, registros |
| `--color-warning` | `#F59E0B` / `#ECC94B` | Contas a vencer, avisos de limite, atenção |

---

## 🏛️ 2. Fundamentos de Arquitetura de Código Frontend

Para garantir excelência de engenharia de software no frontend React:
1. **Estrutura de Pastas Escalável**: Abordagem modular (Vertical Feature-based + Shared Core).
2. **Encapsulamento de Chamadas de API**: Cliente HTTP centralizado (Axios / Fetch) com interceptores para injeção de Bearer JWT, refresh proativo e padronização de erros RFC 7807 (`ProblemDetails`).
3. **State Management & Server State**: Separação clara entre **Server State** (gerenciado via TanStack Query / SWR com caching e invalidação automática) e **Client/UI State** (Zustand ou React Contexts leves).
4. **Hooks Customizados & Separação de Concerns**: Componentes puramente visuais e declarativos (apresentação), com regras de negócio e orquestração de dados encapsuladas em hooks customizados (`useDashboard`, `useTransactions`, `useConsent`).
5. **Component Design & Reusabilidade**: Atomic / Composable design de UI (Button, Input, Modal, Toast/Notification, Select, Table, ChartCard) com acessibilidade (WAI-ARIA).
6. **Sistemas de Feedback (Toasts, Skeletons & Empty States)**: Feedback imediato ao usuário em todas as mutações e carregamentos assíncronos.

---

## 📐 3. Decisões Arquiteturais Confirmadas

### 3.1 Padrão de Arquitetura de Código: Feature-Driven Vertical Slices
- **Estrutura de Pastas**:
  ```text
  src/
  ├── app/                  # Providers globais, roteamento (React Router), layout base
  ├── assets/               # Imagens, fontes, ícones estáticos
  ├── features/             # Módulos verticais de negócio isolados
  │   ├── dashboard/        # Saldo consolidado, métricas, gráficos agregados
  │   │   ├── api/          # Chamadas de API específicas (Typed Fetch/Axios)
  │   │   ├── components/   # Componentes de UI exclusivos da feature
  │   │   ├── hooks/        # Custom hooks e orquestração de queries/mutations
  │   │   ├── types/        # Interfaces e DTOs da feature
  │   │   └── pages/        # Página / View do Dashboard
  │   ├── transactions/     # Extrato, categorização, conciliação e detalhes
  │   ├── consents/         # Gestão de conexões bancárias Open Finance (Itaú, Inter, MP)
  │   ├── budgets/          # Planejamento mensal e orçamentos por categoria
  │   └── calendar/         # Visão mensal de despesas e vencimentos
  └── shared/               # Recursos reutilizáveis e transversais
      ├── api/              # Cliente HTTP centralizado com interceptor JWT e RFC 7807 ProblemDetails
      ├── components/       # Design System base (Button, Input, Modal, Toast, Card, Dropdown, Table)
      ├── hooks/            # Hooks utilitários globais (useToast, useDebounce, useMediaQuery)
      ├── styles/           # Design tokens, variáveis CSS da paleta, reset e tipografia
      ├── types/            # Tipos canônicos compartilhados (ProblemDetails, PaginatedResult, Currency)
      └── utils/            # Formatadores de moeda (BRL), datas (date-fns/Intl), validações
  ```
- **Princípios de Isolamento**: Features não importam código interno de outras features diretamente; qualquer compartilhamento passa pelo `shared/` ou contratos de API.

---

### 3.2 Estratégia de Estilização & Design Tokens
- **Framework**: Tailwind CSS com `clsx` e `tailwind-merge` (função auxiliar `cn(...)`).
- **Design Tokens Integrados**:
  ```typescript
  // Tailwind Theme Configuration / Tokens
  colors: {
    brand: {
      DEFAULT: '#E05697',
      dark: '#941B5C',
      light: '#FCE7F3',
    },
    secondary: {
      DEFAULT: '#1D555A',
      dark: '#164347',
      light: '#E6F4F1',
    },
    tertiary: {
      DEFAULT: '#FF7338',
      light: '#FFF0EA',
    },
    surface: {
      DEFAULT: '#FFFFFF',
      ground: '#F4F7F6',
      subtle: '#EAEFF0',
    },
    status: {
      success: '#2ECC71',
      danger: '#FF5964',
      info: '#38BDF8',
      warning: '#F59E0B',
    }
  }
  ```
- **Utilitários e Componentes**: Estilização declarativa com foco em responsividade, micro-interações suaves de hover/focus e tipografia legível.

---

### 3.3 Camada de API, Autenticação & Server State
- **Cliente HTTP Centralizado (`shared/api/httpClient.ts`)**:
  - Instância do Axios com `baseURL: import.meta.env.VITE_API_GATEWAY_URL || 'http://localhost:5000'`.
  - **Request Interceptor**: Injeta automaticamente o cabeçalho `Authorization: Bearer <token>` a partir da sessão/store de autenticação.
  - **Response Interceptor**: Intercepta respostas de erro (4xx/5xx) e normaliza respostas de acordo com a RFC 7807 (`ProblemDetails`: `type`, `title`, `status`, `detail`, `instance`, `errorCode`, `errors`), repassando um `ApiError` fortemente tipado para a camada de UI.
- **Server State & Caching (`TanStack Query v5`)**:
  - Queries declarativas com chaves padronizadas por domínio (ex: `['dashboard']`, `['transactions', { month, year, category }]`, `['consents']`).
  - Invalidação cirúrgica de cache após mutações (ex: ao criar ou revogar consentimento, invalida `['consents']` e `['dashboard']`).
  - `staleTime` configurado por tipo de dado (dados de extrato e saldos: 1-2 minutos de frescor; consentimentos: 5 minutos).
- **Encapsulamento em Hooks por Feature**:
  - Cada feature expõe hooks limpos que desacoplam a UI dos detalhes de requisição (ex: `useDashboard()`, `useTransactions(filters)`, `useCreateConsent()`).

---

### 3.4 Camada de Feedback do Usuário & Notificações (Toasts)
- **Biblioteca Base**: `Sonner` estilizada para a identidade visual do FinanceHub.
- **Integração com Tratamento de Erros**:
  - Utilitário centralizado `showApiError(error: unknown)` que extrai automaticamente o `detail` ou mensagens de validação da RFC 7807 (`ProblemDetails`) e exibe um toast formatado.
  - Suporte a `toast.promise(...)` para feedback em tempo real durante ações assíncronas (ex: "Sincronizando contas bancárias...", "Autorizando consentimento Open Finance...").
- **Estados Visuais de Feedback**:
  - `toast.success`: Borda sutil verde (`#2ECC71`), ícone de sucesso e mensagem.
  - `toast.error`: Borda vermelha (`#FF5964`), mensagem com código do erro (`errorCode`).
  - `toast.warning`: Alerta de atenção / vencimento de contas.
  - `toast.info`: Mensagens operacionais.

---

### 3.5 Visualização de Dados & Gráficos Financeiros
- **Biblioteca Base**: `Recharts` com componentes desacoplados e responsivos (`ResponsiveContainer`).
- **Gráficos Mapeados da Referência Visual**:
  1. **Donut / Pie Chart (`CategoryExpenseDonut`)**:
     - Visualização da proporção de gastos por categoria (Alimentação, Transporte, Trabalho, Lazer).
     - Tooltip customizado em Tailwind com valor em BRL e percentual formatado.
  2. **Donut de Progresso de Orçamento (`BudgetProgressBarDonut`)**:
     - Exibição de % Gasto vs Restante dentro de cada card de categoria.
  3. **Bar Chart Agrupado (`BudgetVsActualBarChart`)**:
     - Comparativo lado a lado de Orçamento Planejado vs Gasto Realizado por categoria/mês.
  4. **Scatter / Dispersão (`ExpenseScatterPlot`)**:
     - Distribuição de compras ao longo dos dias e faixas de valores para análise de padrões de gastos.

---

### 3.6 Escopo de Páginas, Rotas e Funcionalidades (Foco Core Inicial)
- **Rotas Definidas**:
  | Rota | Feature | Descrição & Componentes |
  | :--- | :--- | :--- |
  | `/login` | `features/auth` | Tela de autenticação baseada em `Login.pdf` com formulário tipado e integração Bearer JWT. |
  | `/` ou `/dashboard` | `features/dashboard` | Visão consolidada multi-bancos: cards de saldo por instituição (Itaú, Inter, Mercado Pago), resumo de entradas/saídas do mês, gráfico Donut de categorias e feed de transações recentes. |
  | `/transacoes` | `features/transactions` | Extrato financeiro unificado: tabela rica com status visual, filtro por período/categoria/banco, busca, visualização de parcelas (`2/5`) e modal de detalhes inspirado em `Modal.pdf`. |
  | `/conexoes` | `features/consents` | Gestão de consentimentos Open Finance: status de conexões bancárias ativas/expiradas, renovação de tokens e fluxo para conectar nova instituição. |

- **Layout Shell (`src/app/layout/`)**:
  - **Sidebar Vertical**: Navegação limpa com ícones (Lucide Icons), logo FinanceHub, indicador do usuário logado e botão de logout.
  - **Topbar Global**: Seleção de mês/ano de referência, campo de pesquisa rápida e indicador de status de conexão com os serviços backend.
  - **Main Container**: Área fluida e responsiva onde as páginas são renderizadas via `<Outlet />`.

---

## 🛠️ 4. Contratos de Integração com o Backend (BFF API Gateway)

O frontend consome exclusivamente o **API Gateway BFF** (`FinanceHub.ApiGateway` na porta `5000`):

1. **Dashboard Agregado**:
   - `GET /api/v1/gateway/dashboard`
   - Retorna: `{ totalBalance, accounts: [{ bank, balance, currency, lastUpdated }], consents: [...], recentTransactions: [...] }`
2. **Extrato e Transações**:
   - `GET /api/v1/transactions?month={m}&year={y}&bank={b}&category={c}`
   - `GET /api/v1/transactions/{id}`
3. **Consentimentos Open Finance**:
   - `GET /api/v1/consents`
   - `POST /api/v1/consents` (Inicia fluxo de autorização FAPI)
   - `POST /api/v1/consents/{id}/revoke` (Revoga consentimento)
4. **Tratamento de Erros RFC 7807**:
   - Respostas `4xx`/`5xx` retornam `application/problem+json`:
     ```json
     {
       "type": "https://financehub.io/errors/CONSENT_EXPIRED",
       "title": "Consent Expired",
       "status": 400,
       "detail": "O consentimento com o banco Itaú expirou e precisa ser renovado.",
       "errorCode": "CONSENT_EXPIRED",
       "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
     }
     ```

---

## 🚀 5. Plano de Execução & Checklist de Implementação

- [ ] **Passo 1 — Scaffolding do Projeto**:
  - Criar projeto Vite + React 18/19 + TypeScript em `src/Web/FinanceHub.Web`.
  - Instalar dependências core: `react-router-dom`, `@tanstack/react-query`, `axios`, `lucide-react`, `recharts`, `sonner`, `clsx`, `tailwind-merge`, `tailwindcss`, `date-fns`.
- [ ] **Passo 2 — Design System & Tokens Tailwind**:
  - Configurar variáveis CSS e cores da paleta no Tailwind (`brand`, `brand-dark`, `secondary`, `tertiary`, `surface`, `status-*`).
  - Configurar tipografia moderna (Google Fonts: Inter / Plus Jakarta Sans) e reset global em `index.css`.
- [ ] **Passo 3 — Primitivas Compartilhadas (`src/shared/`)**:
  - `shared/api/httpClient.ts` com interceptores JWT e RFC 7807 `ProblemDetails`.
  - `shared/components/` (Button, Input, Card, Modal/Dialog, Badge, Table, SkeletonLoader, DropdownSelect).
  - `shared/utils/` (formatadores BRL `formatCurrency`, formatadores de data `formatDate`).
- [ ] **Passo 4 — Layout Shell & Roteamento (`src/app/`)**:
  - Configurar `QueryClientProvider`, `Toaster` do Sonner e `RouterProvider`.
  - Construir `Sidebar`, `Topbar` e `AppLayout`.
- [ ] **Passo 5 — Feature Slices**:
  - **Feature Dashboard**: Hooks `useDashboardQuery`, cards de saldo bancário, gráfico Donut de categorias e resumo financeiro.
  - **Feature Transactions**: Hooks `useTransactionsQuery`, tabela de extrato com filtros, badges de tipo/pagamento e modal de detalhes.
  - **Feature Consents**: Hooks `useConsentsQuery` e `useCreateConsentMutation`, cards de status de bancos conectados (Itaú, Inter, Mercado Pago) e gatilho de conexão.
  - **Feature Auth/Login**: Tela de login com split visual inspirada em `Login.pdf`.
- [ ] **Passo 6 — Validação & Testes**:
  - Testes de componentes (Vitest + Testing Library) para hooks e componentes essenciais.
  - Validação de build sem warnings (`npm run build`).






