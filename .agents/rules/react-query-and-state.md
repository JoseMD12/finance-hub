# TanStack Query & State Management Rules — FinanceHub

> **Target**: `src/Web/finance-hub-web`  
> **Library**: `@tanstack/react-query v5`  
> **Scope**: `Server State Caching, Mutations, Optimistic Updates & Query Keys Factory`

---

## ⚡ 1. Princípio Fundamental de Separação de Estado

1. **Server State (TanStack Query)**:
   - Dados assíncronos que residem no backend (Saldos bancários, Consentimentos Open Finance, Extrato de Transações).
   - Gerenciado exclusivamente via `useQuery` e `useMutation`. NUNCA duplicar dados de API em stores locais (Zustand/Redux) ou em `useState` desnecessários.
2. **Client / UI State**:
   - Estados puramente visuais e transitórios (Modais abertos, abas ativas, filtros de busca temporários).
   - Gerenciado via `useState` local ou React Context/Zustand quando compartilhado entre componentes do mesmo layout.

---

## 🔑 2. Query Keys Factory Pattern Obrigatório

Para evitar chaves mágicas e colisões de cache, toda feature DEVE exportar uma fábrica de chaves fortemente tipada em `features/<feature>/api/<feature>Keys.ts`:

```typescript
// Exemplo canônico: src/features/transactions/api/transactionKeys.ts
export const transactionKeys = {
  all: ['transactions'] as const,
  lists: () => [...transactionKeys.all, 'list'] as const,
  list: (filters: TransactionFiltersDto) => [...transactionKeys.lists(), filters] as const,
  details: () => [...transactionKeys.all, 'detail'] as const,
  detail: (id: string) => [...transactionKeys.details(), id] as const,
};
```

---

## 🕒 3. Políticas de Frescor e Caching (`staleTime` & `gcTime`)

Configurar `staleTime` explícito de acordo com a volatilidade do dado financeiro:

| Tipo de Dado Financeiro | `staleTime` Recomendado | `gcTime` Recomendado | Justificativa |
| :--- | :--- | :--- | :--- |
| **Saldos Consolidados (Dashboard)** | `1 minuto` (`1000 * 60`) | `10 minutos` | Atualizações frequentes via webhooks/ingestão |
| **Extrato de Transações** | `2 minutos` (`1000 * 60 * 2`) | `15 minutos` | Dados imutáveis após ingestão |
| **Consentimentos Open Finance** | `5 minutos` (`1000 * 60 * 5`) | `30 minutos` | Mudanças raras (apenas ao conectar/revogar) |

---

## 🔄 4. Padrão para Custom Hooks de Query e Mutation

Toda chamada de API deve ser envelopada em um custom hook tipado com `ApiError`:

```typescript
// src/features/transactions/hooks/useTransactionsQuery.ts
import { useQuery } from '@tanstack/react-query';
import { transactionKeys } from '../api/transactionKeys';
import { getTransactionsApi } from '../api/transactionsApi';
import type { TransactionFiltersDto, PaginatedTransactionsDto } from '../types/transactions.types';
import type { ApiError } from '@/shared/types/api.types';

export function useTransactionsQuery(filters: TransactionFiltersDto) {
  return useQuery<PaginatedTransactionsDto, ApiError>({
    queryKey: transactionKeys.list(filters),
    queryFn: ({ signal }) => getTransactionsApi(filters, signal),
    staleTime: 1000 * 60 * 2, // 2 minutos
    placeholderData: (previousData) => previousData, // Mantém paginação fluida sem layout shift
  });
}
```

---

## 🚀 5. Mutações, Invalidações e Optimistic Updates

### 5.1 Mutação Padrão com Feedback e Invalidação Cirúrgica
```typescript
// src/features/consents/hooks/useRevokeConsentMutation.ts
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { consentKeys } from '../api/consentKeys';
import { revokeConsentApi } from '../api/consentsApi';
import { showApiError } from '@/shared/utils/apiError';
import type { ApiError } from '@/shared/types/api.types';

export function useRevokeConsentMutation() {
  const queryClient = useQueryClient();

  return useMutation<void, ApiError, string>({
    mutationFn: (consentId: string) => revokeConsentApi(consentId),
    onSuccess: () => {
      toast.success('Consentimento revogado com sucesso!');
      queryClient.invalidateQueries({ queryKey: consentKeys.all });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
    },
    onError: (error) => {
      showApiError(error);
    },
  });
}
```

### 5.2 Mutação Otimista (Optimistic Update com Rollback)
Para operações de alta interatividade (ex: trocar categoria de uma transação ou editar uma tag):

```typescript
// src/features/transactions/hooks/useUpdateTransactionCategoryMutation.ts
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { transactionKeys } from '../api/transactionKeys';
import { updateTransactionCategoryApi } from '../api/transactionsApi';
import { showApiError } from '@/shared/utils/apiError';
import type { PaginatedTransactionsDto, TransactionDto } from '../types/transactions.types';
import type { ApiError } from '@/shared/types/api.types';

export function useUpdateTransactionCategoryMutation(filters: any) {
  const queryClient = useQueryClient();

  return useMutation<TransactionDto, ApiError, { transactionId: string; newCategory: string }>({
    mutationFn: ({ transactionId, newCategory }) =>
      updateTransactionCategoryApi(transactionId, newCategory),
    onMutate: async ({ transactionId, newCategory }) => {
      // 1. Cancela refetches pendentes para não sobrescrever o snapshot
      await queryClient.cancelQueries({ queryKey: transactionKeys.list(filters) });

      // 2. Snapshot do estado anterior para rollback em caso de falha
      const previousData = queryClient.getQueryData<PaginatedTransactionsDto>(transactionKeys.list(filters));

      // 3. Atualização otimista no cache
      if (previousData) {
        queryClient.setQueryData<PaginatedTransactionsDto>(transactionKeys.list(filters), {
          ...previousData,
          items: previousData.items.map((item) =>
            item.id === transactionId ? { ...item, category: newCategory } : item
          ),
        });
      }

      return { previousData };
    },
    onError: (err, _, context) => {
      // Rollback se a requisição falhar
      if (context?.previousData) {
        queryClient.setQueryData(transactionKeys.list(filters), context.previousData);
      }
      showApiError(err, 'Não foi possível alterar a categoria.');
    },
    onSettled: () => {
      // Revalida para sincronizar com o backend
      queryClient.invalidateQueries({ queryKey: transactionKeys.list(filters) });
    },
  });
}
```

