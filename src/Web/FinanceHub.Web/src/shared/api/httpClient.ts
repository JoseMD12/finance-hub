import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios';
import { ApiError, type ProblemDetails } from '../types/api.types';
import { getAccessToken, setAccessToken, clearSession } from '@/shared/utils/authStorage';

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

// Response Interceptor: Normaliza RFC 7807 e gerencia fila de Refresh (anti-race condition)
httpClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

    // Tratamento de 401 com fila de espera
    if (error.response?.status === 401 && originalRequest && !originalRequest._retry) {
      if (originalRequest.url?.includes('/api/v1/auth/login') || originalRequest.url?.includes('/api/v1/auth/refresh')) {
        throw normalizeError(error);
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
          .catch((err: unknown) => { throw err; });
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
        throw normalizeError(refreshErr);
      } finally {
        isRefreshing = false;
      }
    }

    throw normalizeError(error);
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
