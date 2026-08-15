# React & Vite Frontend Architecture Rules — FinanceHub

> **Target**: `src/Web/finance-hub-web`  
> **Framework**: `React 19 / 18 + Vite + TypeScript`  
> **Pattern**: `Feature-Driven Vertical Slices + Shared Core`

---

## 🏛️ 1. Estrutura Modular & Feature-Driven Vertical Slices

O frontend do FinanceHub adota a **Feature-Driven Architecture (Vertical Slices)**. Cada domínio de negócio é autocontido e isolado.

```text
src/
├── app/                  # Bootstrap da aplicação, rotas globais, providers e layout base
│   ├── layout/           # AppLayout, Sidebar, Topbar
│   ├── providers/        # QueryClientProvider, ToastProvider, RouterProvider
│   └── routes/           # Configuração do React Router (AppRoutes.tsx)
├── assets/               # Imagens estáticas, SVGs, ícones e fontes
├── features/             # Módulos verticais de negócio isolados
│   ├── auth/             # Login, sessão e autenticação Bearer
│   ├── dashboard/        # Saldo consolidado, métricas multi-bancos, gráficos agregados
│   ├── transactions/     # Extrato, categorização, conciliação e detalhes de parcelas
│   └── consents/         # Gestão de conexões Open Finance (Itaú, Inter, Mercado Pago)
└── shared/               # Recursos transversais e Design System
    ├── api/              # Cliente HTTP centralizado (Axios) com interceptor RFC 7807
    ├── components/       # Design System UI (Button, Input, Modal, Badge, Table, Card)
    ├── hooks/            # Hooks utilitários globais (useToast, useDebounce, useMediaQuery)
    ├── styles/           # Tokens CSS, variáveis globais, resets e Tailwind directives
    ├── types/            # DTOs e tipos canônicos (ProblemDetails, Currency, BankEnum)
    └── utils/            # Formatadores (BRL, datas, máscara de CPF/contas)
```

---

## 📦 2. Estrutura Interna de Cada Feature (`src/features/<nome>/`)

Toda pasta de feature DEVE seguir rigorosamente esta organização interna:

```text
src/features/<feature-name>/
├── api/                  # Funções assíncronas de API (Axios) e Query Keys
│   ├── <feature>Api.ts
│   └── <feature>Keys.ts
├── components/           # Componentes de UI exclusivos desta feature
│   ├── <Feature>Card.tsx
│   └── <Feature>FilterModal.tsx
├── hooks/                # Custom hooks (orquestração de queries, mutations e estado local)
│   ├── use<Feature>Query.ts
│   └── use<Feature>Mutation.ts
├── types/                # Interfaces TypeScript e DTOs específicos da feature
│   └── <feature>.types.ts
├── pages/                # Componente de Página / View exportado para as rotas
│   └── <Feature>Page.tsx
├── __tests__/            # Testes automatizados (Vitest + Testing Library + MSW)
│   ├── <Feature>Page.test.tsx
│   └── <feature>Mocks.ts
└── index.ts              # BARREIRA PÚBLICA (Public API Boundary)
```

### 🚫 Regras de Isolamento entre Features & Public API Boundary
1. **Public API Boundary (`index.ts`)**: Cada feature DEVE expor um `index.ts` na raiz da feature, definindo estritamente o que pode ser consumido externamente (geralmente apenas o componente da Página e tipos públicos).
2. **Sem Deep Imports**: É proibido importar subpastas internas de outra feature (ex: `import ... from '@/features/auth/api/authApi'`).
3. **Compartilhamento via `shared/`**: Se um componente, utilitário ou tipo for necessário em mais de uma feature, ele DEVE ser promovido para `src/shared/`.

---

## 📝 3. Padrão Unificado de Formulários & Validação

Para evitar divergência de bibliotecas entre features:
1. **Biblioteca Oficial**: Todos os formulários DEVEM utilizar `react-hook-form` integrado ao `@hookform/resolvers/zod` com schemas do `zod`.
2. **Mapeamento de Erros do Backend**: Em caso de falha de validação da API (RFC 7807), utilize obrigatoriamente `mapProblemDetailsToFormErrors(error, setError)` para marcar os campos incorretos no formulário com suas mensagens nativas.

---

## ⚡ 4. Code-Splitting & Lazy Loading nas Rotas

1. **Lazy Loading de Páginas**:
   - Todas as páginas importadas em `AppRoutes.tsx` DEVEM utilizar `React.lazy()` para garantir bundles leves e carregamento instantâneo.
2. **Suspense com Skeleton**:
   - Cada rota dinâmica deve ser envelopada por `<Suspense fallback={<PageSkeleton />}>`.

---

## 🧩 5. Separação Estrita de Responsabilidades (Smart vs Dumb Components)

1. **Apresentação Pura (Dumb / Presentational Components)**:
   - Residem em `features/<feature>/components/` ou `shared/components/`.
   - **Regra**: Devem ser funções puras recebendo dados e callbacks via `props`. NUNCA devem disparar requisições HTTP ou instanciar `useQuery`/`useMutation` diretamente.
2. **Containers / Hooks de Negócio (Smart Logic)**:
   - Residem em `features/<feature>/hooks/` e `features/<feature>/pages/`.
   - **Regra**: Encapsulam a busca de dados, gerenciamento de estado do formulário, debounce e mutações com feedback via Toast.

---

## 🔒 6. Tipagem Estrita TypeScript (Zero `any`)

1. **Sem `any` ou `unknown` sem Type Guard**:
   - Todo payload de API, parâmetro de função e prop de componente DEVE possuir interface explícita.
   - Proibido o uso de `as any` ou type casting forçado.
2. **Nomenclatura de Tipos e Props**:
   - Nomes de interfaces de props: `<NomeComponente>Props` (ex: `TransactionTableProps`).
   - Nomes de DTOs de resposta da API: `<Entidade>ResponseDto` ou `<Entidade>Dto`.
3. **Imutabilidade e Readonly**:
   - Prefira `readonly` em arrays e tipos de estado imutáveis (`readonly TransactionDto[]`).

---

## 🔄 7. Ciclo de Vida e Resiliência de Componentes

1. **Estados Obrigatórios de Carregamento**:
   - Todo componente assíncrono DEVE tratar os 3 estados canônicos:
     - `isLoading`: Exibir Skeleton Loader adaptado ao layout.
     - `isError`: Exibir Empty/Error State com mensagem amigável e botão de "Tentar Novamente".
     - `data`: Renderizar a UI normalizada com formatação monetária e de datas.
2. **Cleanups em useEffect**:
   - Qualquer subscription, timer (`setTimeout`/`setInterval`) ou listener de eventos DEVE possuir função de cleanup retornada no `useEffect`.

