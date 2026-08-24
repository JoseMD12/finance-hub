import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { TransactionsPage } from '../pages/TransactionsPage';
import * as transactionsApi from '../api/transactionsApi';
import type { PaginatedTransactionsDto, CategoryDto } from '../types/transactions.types';

vi.mock('../api/transactionsApi');

const mockCategories: CategoryDto[] = [
  {
    id: '11111111-1111-1111-1111-111111111101',
    name: 'Alimentação',
    slug: 'food',
    iconKey: 'utensils',
    colorToken: 'emerald',
    isSystemDefault: true,
    isActive: true,
    subcategories: [
      {
        id: '22222222-2222-2222-2222-222222222201',
        name: 'Supermercado',
        slug: 'food-supermarket',
        parentCategoryId: '11111111-1111-1111-1111-111111111101',
        iconKey: 'shopping-bag',
        colorToken: 'emerald',
        isSystemDefault: true,
        isActive: true,
      },
    ],
  },
];

const mockTransactionsData: PaginatedTransactionsDto = {
  items: [
    {
      id: 'tx-1',
      userId: 'user-1',
      institutionId: 'itau',
      accountNumber: '12345-6',
      amount: 150.0,
      currency: 'BRL',
      type: 'Debit',
      description: 'Supermercado Silva',
      categoryId: '11111111-1111-1111-1111-111111111101',
      categorizationSource: 'GlobalPattern',
      isManuallyCategorized: false,
      transactionDateUtc: '2026-08-20T10:00:00Z',
      channel: 'Pix',
      merchantName: 'Silva Supermercado',
    },
  ],
  summary: {
    totalIncome: 0,
    totalExpense: 150.0,
    netBalance: -150.0,
    totalCount: 1,
  },
  page: 1,
  pageSize: 20,
  totalItems: 1,
  totalPages: 1,
};

const createTestQueryClient = () =>
  new QueryClient({
    defaultOptions: {
      queries: { retry: false },
    },
  });

describe('TransactionsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(transactionsApi.getTransactionsApi).mockResolvedValue(mockTransactionsData);
    vi.mocked(transactionsApi.getCategoriesApi).mockResolvedValue(mockCategories);
    vi.mocked(transactionsApi.categorizeTransactionApi).mockResolvedValue();
  });

  it('deve renderizar o título, resumo do período e tabela de transações', async () => {
    const queryClient = createTestQueryClient();
    render(
      <QueryClientProvider client={queryClient}>
        <TransactionsPage />
      </QueryClientProvider>
    );

    expect(screen.getByText('Extrato de Transações')).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByText('Supermercado Silva')).toBeInTheDocument();
      expect(screen.getByText('Alimentação')).toBeInTheDocument();
      expect(screen.getAllByText(/150,00/).length).toBeGreaterThan(0);
    });
  });

  it('deve abrir o modal de detalhes ao clicar no botão de visualização', async () => {
    const user = userEvent.setup();
    const queryClient = createTestQueryClient();
    render(
      <QueryClientProvider client={queryClient}>
        <TransactionsPage />
      </QueryClientProvider>
    );

    await waitFor(() => {
      expect(screen.getByText('Supermercado Silva')).toBeInTheDocument();
    });

    const actionMenuButton = screen.getByLabelText('Ações da transação Supermercado Silva');
    await user.click(actionMenuButton);

    const viewButton = screen.getByRole('menuitem', { name: /ver detalhes/i });
    await user.click(viewButton);

    expect(screen.getByText('Detalhes da Transação')).toBeInTheDocument();
    expect(screen.getByText('Meio de Pagamento')).toBeInTheDocument();
    expect(screen.getAllByText('Pix').length).toBeGreaterThan(0);
  });

  it('deve abrir o dropdown hierárquico de categoria na barra de filtros', async () => {
    const user = userEvent.setup();
    const queryClient = createTestQueryClient();
    render(
      <QueryClientProvider client={queryClient}>
        <TransactionsPage />
      </QueryClientProvider>
    );

    await waitFor(() => {
      expect(screen.getByText('Todas as Categorias')).toBeInTheDocument();
    });

    const categorySelectTrigger = screen.getByRole('button', { name: 'Categoria' });
    await user.click(categorySelectTrigger);

    expect(screen.getByPlaceholderText('Buscar categoria ou subcategoria...')).toBeInTheDocument();
  });

  it('deve abrir o popover de alteração de categoria inline na tabela', async () => {
    const user = userEvent.setup();
    const queryClient = createTestQueryClient();
    render(
      <QueryClientProvider client={queryClient}>
        <TransactionsPage />
      </QueryClientProvider>
    );

    await waitFor(() => {
      expect(screen.getByText('Supermercado Silva')).toBeInTheDocument();
    });

    const categoryTag = screen.getByRole('button', { name: /categoria: alimentação/i });
    await user.click(categoryTag);

    expect(screen.getByText('Alterar Categoria')).toBeInTheDocument();
    expect(screen.getByLabelText('Alterar categoria da transação')).toBeInTheDocument();
  });

  it('deve alternar a neutralidade da transação via toggle no modal de detalhes', async () => {
    const user = userEvent.setup();
    const queryClient = createTestQueryClient();
    vi.mocked(transactionsApi.toggleTransactionNeutralityApi).mockResolvedValue();

    render(
      <QueryClientProvider client={queryClient}>
        <TransactionsPage />
      </QueryClientProvider>
    );

    await waitFor(() => {
      expect(screen.getByText('Supermercado Silva')).toBeInTheDocument();
    });

    const actionMenuButton = screen.getByLabelText('Ações da transação Supermercado Silva');
    await user.click(actionMenuButton);

    const viewButton = screen.getByRole('menuitem', { name: /ver detalhes/i });
    await user.click(viewButton);

    const toggleButton = screen.getByRole('button', { name: 'Alternar neutralidade nos totais' });
    expect(toggleButton).toHaveAttribute('aria-pressed', 'false');

    await user.click(toggleButton);

    expect(transactionsApi.toggleTransactionNeutralityApi).toHaveBeenCalledWith({
      transactionId: 'tx-1',
      isIgnoredInTotals: true,
      reason: 'Marcado manualmente como neutro/trânsito',
    });
  });
});
