# HTTP Client, JWT Security & RFC 7807 Exception Rules — FinanceHub Frontend

> **Target**: `src/shared/api/httpClient.ts` & `src/shared/types/api.types.ts`  
> **Standard**: `RFC 7807 (ProblemDetails for HTTP APIs)` + `Bearer JWT Authentication with Silent Refresh Queue`

---

## 🔒 1. Segurança de Tokens JWT & Arquitetura de Sessão

1. **Armazenamento de Tokens**:
   - **Access Token**: Armazenado estritamente em **memória JavaScript** (`authStore` / closure em memória) para mitigar riscos de exfiltração via XSS.
   - **Refresh Token**: Gerenciado preferencialmente via Cookies `HttpOnly; Secure; SameSite=Strict` ou storage seguro de sessão com rotação a cada uso.
   - **Zero Secrets em Storage**: Proibido armazenar credenciais bancárias, chaves de API ou tokens de consentimento descriptografados no `localStorage`.

---

## 🌐 2. Cliente HTTP Centralizado (`httpClient.ts`) & Fila de 401

Todas as requisições HTTP utilizam a instância do Axios em `src/shared/api/httpClient.ts`.

### 2.1 Resiliência contra Concorrência no 401 (*Silent Refresh Queue*)
Para evitar loops de 401 e logout precipitado quando múltiplas requisições paralelas falham ao mesmo tempo (ex: ao abrir o Dashboard):

```typescript
// src/shared/api/httpClient.ts
import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios';
import { ApiError, type ProblemDetails } from '../types/api.types';
import { getAccessToken, setAccessToken, clearSession } from '@/features/auth/utils/authStorage';

let isRefreshing = false;
let failedQueue: Array<{
  resolve: (token: string) => void;
  reject: (error: unknown) => void;
}> = [];

const processQueue = (error: unknown, token: string | null = null) => {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error);
    } else if (token) {
      prom.resolve(token);
    }
  });
  failedQueue = [];
};

export const httpClient = axios.create({
  baseURL: import.meta.env.VITE_API_GATEWAY_URL || 'http://localhost:5000',
  timeout: 15000,
  headers: {
    'Content-Type': 'application/json',
    Accept: 'application/json, application/problem+json',
  },
});

// Request Interceptor: Injeta Bearer Token e Correlation ID
httpClient.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = getAccessToken();
  if (token && config.headers) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  if (config.headers && !config.headers['X-Correlation-Id']) {
    config.headers['X-Correlation-Id'] = crypto.randomUUID();
  }
  return config;
});

// Response Interceptor: Normaliza RFC 7807 e gerencia fila de Refresh
httpClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

    // Tratamento de 401 com fila de espera
    if (error.response?.status === 401 && originalRequest && !originalRequest._retry) {
      // Ignora tentativa de refresh na própria rota de login/refresh
      if (originalRequest.url?.includes('/api/v1/auth/login') || originalRequest.url?.includes('/api/v1/auth/refresh')) {
        return Promise.reject(normalizeError(error));
      }

      if (isRefreshing) {
        return new Promise<string>((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        })
          .then((token) => {
            if (originalRequest.headers) {
              originalRequest.headers.Authorization = `Bearer ${token}`;
            }
            return httpClient(originalRequest);
          })
          .catch((err) => Promise.reject(err));
      }

      originalRequest._retry = true;
      isRefreshing = true;

      try {
        const refreshResponse = await httpClient.post<{ accessToken: string }>('/api/v1/auth/refresh');
        const newToken = refreshResponse.data.accessToken;
        setAccessToken(newToken);
        processQueue(null, newToken);

        if (originalRequest.headers) {
          originalRequest.headers.Authorization = `Bearer ${newToken}`;
        }
        return httpClient(originalRequest);
      } catch (refreshErr) {
        processQueue(refreshErr, null);
        clearSession();
        if (window.location.pathname !== '/login') {
          window.location.href = '/login';
        }
        return Promise.reject(normalizeError(refreshErr));
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(normalizeError(error));
  }
);

function normalizeError(error: unknown): ApiError {
  if (axios.isAxiosError(error) && error.response?.data) {
    const data = error.response.data as Partial<ProblemDetails>;
    const problem: ProblemDetails = {
      type: data.type || 'https://financehub.io/errors/INTERNAL_ERROR',
      title: data.title || 'Falha na Operação',
      status: error.response.status,
      detail: data.detail || (typeof error.response.data === 'string' ? error.response.data : undefined),
      errorCode: data.errorCode || `HTTP_${error.response.status}`,
      traceId: data.traceId,
      errors: data.errors,
    };
    return new ApiError(problem);
  }

  return new ApiError({
    title: 'Erro de Conexão',
    status: 503,
    detail: 'Não foi possível conectar aos serviços do FinanceHub.',
    errorCode: 'NETWORK_UNAVAILABLE',
  });
}
```

---

## 🛡️ 3. Estrutura Canônica RFC 7807 (`ProblemDetails`) & Utilitários

```typescript
// src/shared/types/api.types.ts
export interface ProblemDetails {
  readonly type?: string;
  readonly title: string;
  readonly status: number;
  readonly detail?: string;
  readonly instance?: string;
  readonly errorCode?: string;
  readonly traceId?: string;
  readonly errors?: Record<string, string[]>; // FluentValidation do .NET 10
}

export class ApiError extends Error {
  readonly problemDetails: ProblemDetails;
  readonly status: number;
  readonly errorCode?: string;

  constructor(problem: ProblemDetails) {
    super(problem.detail || problem.title || 'Ocorreu um erro na requisição.');
    this.name = 'ApiError';
    this.problemDetails = problem;
    this.status = problem.status;
    this.errorCode = problem.errorCode;
  }
}
```

### 3.1 Mapeador de Erros para Formulários (React Hook Form / Zod)

```typescript
// src/shared/utils/formErrors.ts
import { ApiError } from '../types/api.types';
import type { UseFormSetError, FieldValues, Path } from 'react-hook-form';

/**
 * Mapeia erros campo a campo do FluentValidation (.NET 10) para o React Hook Form,
 * normalizando automaticamente entre PascalCase do backend e camelCase do form.
 */
export function mapProblemDetailsToFormErrors<T extends FieldValues>(
  error: unknown,
  setError: UseFormSetError<T>
): boolean {
  if (error instanceof ApiError && error.problemDetails.errors) {
    const { errors } = error.problemDetails;
    let hasFieldErrors = false;

    Object.entries(errors).forEach(([field, messages]) => {
      if (messages && messages.length > 0) {
        // Converte PascalCase para camelCase (ex: BankId -> bankId)
        const normalizedFieldName = (field.charAt(0).toLowerCase() + field.slice(1)) as Path<T>;
        setError(normalizedFieldName, {
          type: 'server',
          message: messages[0],
        });
        hasFieldErrors = true;
      }
    });

    return hasFieldErrors;
  }

  return false;
}
```

### 3.2 Notificação Visual com Toast (`showApiError`)

```typescript
// src/shared/utils/apiError.ts
import { toast } from 'sonner';
import { ApiError } from '../types/api.types';

export function showApiError(error: unknown, fallbackMessage = 'Erro ao processar operação.') {
  if (error instanceof ApiError) {
    const { title, detail, errorCode } = error.problemDetails;
    toast.error(title || 'Falha na Operação', {
      description: detail || fallbackMessage,
      action: errorCode ? { label: `Info: ${errorCode}`, onClick: () => {} } : undefined,
    });
    return;
  }

  toast.error(fallbackMessage);
}
```

