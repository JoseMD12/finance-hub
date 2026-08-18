export interface TransactionDto {
  id: string;
  description: string;
  category: string;
  amount: number;
  type: 'INCOME' | 'EXPENSE';
  paymentMethod: string;
  date: string;
  bank: string;
  accountNumber: string;
  installment?: string;
}

export interface PaginatedTransactionsDto {
  items: TransactionDto[];
  page: number;
  pageSize: number;
  totalItems: number;
}
