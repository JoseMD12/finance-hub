---
name: scaffold-frontend-feature
description: Guide for scaffolding complete, modular Feature Slices in React + Vite + TypeScript for FinanceHub, following Vertical Slice Architecture, TanStack Query, typed Axios clients, and RFC 7807 error handling.
---

# Frontend Feature Slicing Skill — FinanceHub (React + Vite)

## ⚡ Trigger / Slash Command
```bash
/scaffold-frontend-feature <FeatureName>
```

Use esta habilidade para criar uma nova fatia vertical de funcionalidade (Feature Slice) no frontend React (`src/Web/finance-hub-web`) com suporte a testes automatizados (Vitest + Testing Library + MSW) e TDD.

---

## 🏛️ 1. Directory Structure Conventions

Cada nova funcionalidade DEVE ser criada dentro de `src/features/<featureName>/`:

```text
src/features/<featureName>/
├── api/
│   ├── <featureName>Api.ts       # Funções assíncronas do Axios
│   └── <featureName>Keys.ts      # Query Keys Factory
├── components/
│   ├── <FeatureName>Card.tsx     # Componentes de apresentação puros
│   └── <FeatureName>Form.tsx     # Formulário tipado com react-hook-form + zod
├── hooks/
│   ├── use<FeatureName>Query.ts  # Custom Hook de Query (TanStack Query)
│   └── use<FeatureName>Mutation.ts # Custom Hook de Mutation com Toasts
├── types/
│   └── <featureName>.types.ts    # DTOs e interfaces TypeScript
├── pages/
│   └── <FeatureName>Page.tsx     # Página principal da rota
├── __tests__/
│   ├── <FeatureName>Page.test.tsx # Teste unitário e de integração (Vitest + RTL)
│   └── <featureName>Mocks.ts     # Handlers MSW e dados mockados
└── index.ts                      # Public API Boundary (exporta Page e DTOs)
```

---

## 📋 2. Step-by-Step Feature Scaffolding Workflow

### Passo 1: Definir os Tipos e DTOs (`types/<featureName>.types.ts`)
```typescript
export interface <FeatureName>ItemDto {
  readonly id: string;
  readonly name: string;
  readonly amount: number;
  readonly createdAt: string;
}

export interface <FeatureName>FiltersDto {
  readonly search?: string;
  readonly month?: number;
  readonly year?: number;
}
```

### Passo 2: Definir as Query Keys (`api/<featureName>Keys.ts`)
```typescript
import type { <FeatureName>FiltersDto } from '../types/<featureName>.types';

export const <featureName>Keys = {
  all: ['<featureName>'] as const,
  lists: () => [...<featureName>Keys.all, 'list'] as const,
  list: (filters: <FeatureName>FiltersDto) => [...<featureName>Keys.lists(), filters] as const,
  details: () => [...<featureName>Keys.all, 'detail'] as const,
  detail: (id: string) => [...<featureName>Keys.details(), id] as const,
};
```

### Passo 3: Criar as Chamadas de API (`api/<featureName>Api.ts`)
```typescript
import { httpClient } from '@/shared/api/httpClient';
import type { <FeatureName>ItemDto, <FeatureName>FiltersDto } from '../types/<featureName>.types';

export async function get<FeatureName>ListApi(
  filters: <FeatureName>FiltersDto,
  signal?: AbortSignal
): Promise<<FeatureName>ItemDto[]> {
  const response = await httpClient.get<<FeatureName>ItemDto[]>('/api/v1/<featureName>', {
    params: filters,
    signal,
  });
  return response.data;
}

export async function create<FeatureName>Api(
  payload: Omit<<FeatureName>ItemDto, 'id' | 'createdAt'>
): Promise<<FeatureName>ItemDto> {
  const response = await httpClient.post<<FeatureName>ItemDto>('/api/v1/<featureName>', payload);
  return response.data;
}
```

### Passo 4: Criar os Custom Hooks (`hooks/`)
```typescript
// use<FeatureName>Query.ts
import { useQuery } from '@tanstack/react-query';
import { <featureName>Keys } from '../api/<featureName>Keys';
import { get<FeatureName>ListApi } from '../api/<featureName>Api';
import type { <FeatureName>FiltersDto, <FeatureName>ItemDto } from '../types/<featureName>.types';
import type { ApiError } from '@/shared/types/api.types';

export function use<FeatureName>Query(filters: <FeatureName>FiltersDto) {
  return useQuery<<FeatureName>ItemDto[], ApiError>({
    queryKey: <featureName>Keys.list(filters),
    queryFn: ({ signal }) => get<FeatureName>ListApi(filters, signal),
    staleTime: 1000 * 60 * 2,
  });
}
```

```typescript
// use<FeatureName>Mutation.ts
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { <featureName>Keys } from '../api/<featureName>Keys';
import { create<FeatureName>Api } from '../api/<featureName>Api';
import { showApiError } from '@/shared/utils/apiError';
import type { <FeatureName>ItemDto } from '../types/<featureName>.types';
import type { ApiError } from '@/shared/types/api.types';

export function use<FeatureName>Mutation() {
  const queryClient = useQueryClient();

  return useMutation<<FeatureName>ItemDto, ApiError, Omit<<FeatureName>ItemDto, 'id' | 'createdAt'>>({
    mutationFn: (payload) => create<FeatureName>Api(payload),
    onSuccess: () => {
      toast.success('Item criado com sucesso!');
      queryClient.invalidateQueries({ queryKey: <featureName>Keys.all });
    },
    onError: (error) => {
      showApiError(error);
    },
  });
}
```

### Passo 5: Criar os Testes Automatizados TDD (`__tests__/`)
```typescript
// src/features/<featureName>/__tests__/<FeatureName>Page.test.tsx
import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { <FeatureName>Page } from '../pages/<FeatureName>Page';

const createTestQueryClient = () =>
  new QueryClient({
    defaultOptions: {
      queries: { retry: false },
    },
  });

describe('<FeatureName>Page', () => {
  it('deve exibir o Skeleton de carregamento inicialmente', () => {
    const queryClient = createTestQueryClient();
    render(
      <QueryClientProvider client={queryClient}>
        <<FeatureName>Page />
      </QueryClientProvider>
    );

    expect(screen.getByTestId('loading-skeleton')).toBeInTheDocument();
  });
});
```

### Passo 6: Exportar a Barreira Pública (`index.ts`)
```typescript
// src/features/<featureName>/index.ts
export { <FeatureName>Page as default } from './pages/<FeatureName>Page';
export type * from './types/<featureName>.types';
```

---

## 🔍 3. Checklist de Validação da Feature

- [ ] Zero uso de `any` no TypeScript.
- [ ] Chaves de cache gerenciadas exclusivamente via `<featureName>Keys`.
- [ ] Erros de formulário mapeados com `mapProblemDetailsToFormErrors` e Toasts com `showApiError` (RFC 7807).
- [ ] Valores monetários formatados via `formatCurrencyBRL` singleton.
- [ ] Testes unitários em `__tests__/` passando 100% com Vitest.
- [ ] Exportação estrita através de `src/features/<featureName>/index.ts`.

