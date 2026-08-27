import { httpClient } from '@/shared/api/httpClient';
import { API_ENDPOINTS } from '@/shared/api/apiEndpoints';
import type {
  CategoryDto,
  CategorizeTransactionPayload,
  PaginatedTransactionsDto,
  TransactionFilterParams,
  ToggleBillPaymentPayload,
} from '../types/transactions.types';

export const getTransactionsApi = async (
  filters: TransactionFilterParams = {},
  signal?: AbortSignal
): Promise<PaginatedTransactionsDto> => {
  const response = await httpClient.get<PaginatedTransactionsDto>(API_ENDPOINTS.TRANSACTIONS.LIST, {
    params: filters,
    signal,
  });
  return response.data;
};

export const getCategoriesApi = async (signal?: AbortSignal): Promise<CategoryDto[]> => {
  const response = await httpClient.get<CategoryDto[]>(API_ENDPOINTS.TRANSACTIONS.CATEGORIES, {
    signal,
  });
  return response.data;
};

export const categorizeTransactionApi = async (
  payload: CategorizeTransactionPayload
): Promise<void> => {
  await httpClient.patch(API_ENDPOINTS.TRANSACTIONS.CATEGORIZE(payload.transactionId), {
    categoryId: payload.categoryId,
    createCustomRule: payload.createCustomRule,
    applyToPastTransactions: payload.applyToPastTransactions ?? false,
  });
};

export const toggleTransactionNeutralityApi = async (
  payload: { transactionId: string; isIgnoredInTotals: boolean; reason?: string }
): Promise<void> => {
  await httpClient.patch(API_ENDPOINTS.TRANSACTIONS.TOGGLE_NEUTRALITY(payload.transactionId), {
    isIgnoredInTotals: payload.isIgnoredInTotals,
    reason: payload.reason,
  });
};

export const toggleTransactionBillPaymentApi = async (
  payload: ToggleBillPaymentPayload
): Promise<void> => {
  await httpClient.patch(API_ENDPOINTS.TRANSACTIONS.TOGGLE_BILL_PAYMENT(payload.transactionId), {
    isBillPayment: payload.isBillPayment,
  });
};

export const updateTransactionNotesApi = async (
  payload: { transactionId: string; notes: string | null }
): Promise<void> => {
  await httpClient.patch(API_ENDPOINTS.TRANSACTIONS.UPDATE_NOTES(payload.transactionId), {
    notes: payload.notes,
  });
};
