import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { DashboardPage } from '../pages/DashboardPage';
import * as dashboardApi from '../api/dashboardApi';
import { ApiError } from '@/shared/types/api.types';
import type { DashboardSummaryDto } from '../types/dashboard.types';

vi.mock('../api/dashboardApi');

const mockDashboard: DashboardSummaryDto = {
  userId: 'user-1',
  summary: {
    totalIncome: 8500,
    totalExpense: 5400,
    netBalance: 3100,
    totalCount: 42,
    realConsolidatedBalanceBrl: 1650.59,
    totalOpenCreditCardsBrl: 4316.78,
    projectedAvailableBalanceBrl: -2666.19,
    lastSyncAtUtc: '2026-08-27T12:00:00Z',
  },
  institutionBalances: [
    {
      institutionId: 'itau',
      accountNumber: 'acc-1',
      balanceBrl: 1650.59,
      currency: 'BRL',
      isCreditCard: false,
      creditLimit: null,
      availableCreditLimit: null,
      usedCreditLimit: null,
      invoiceDueDateUtc: null,
      lastUpdatedAtUtc: '2026-08-27T12:00:00Z',
    },
    {
      institutionId: 'inter',
      accountNumber: 'card-1',
      balanceBrl: -1117.5,
      currency: 'BRL',
      isCreditCard: true,
      creditLimit: 4500,
      availableCreditLimit: 3382.5,
      usedCreditLimit: 1117.5,
      invoiceDueDateUtc: '2026-09-12T00:00:00Z',
      lastUpdatedAtUtc: '2026-08-27T12:00:00Z',
    },
  ],
  categoryExpenses: [
    {
      categoryId: 'cat-1',
      categoryName: 'Alimentação',
      colorToken: 'emerald',
      iconKey: 'utensils',
      amountBrl: 3200,
      percentage: 59.26,
    },
    {
      categoryId: 'cat-2',
      categoryName: 'Transporte',
      colorToken: 'sky',
      iconKey: 'car',
      amountBrl: 2200,
      percentage: 40.74,
    },
  ],
  monthlyCashFlow: [
    { year: 2026, month: 7, incomeBrl: 8000, expenseBrl: 5000, netBrl: 3000 },
    { year: 2026, month: 8, incomeBrl: 8500, expenseBrl: 5400, netBrl: 3100 },
  ],
  recentTransactions: [
    {
      id: 'tx-1',
      userId: 'user-1',
      institutionId: 'itau',
      accountNumber: 'acc-1',
      amount: 120.5,
      currency: 'BRL',
      type: 'Debit',
      description: 'SUPERMERCADO CARREFOUR',
      categoryId: 'cat-1',
      categorizationSource: 'GlobalRule',
      isManuallyCategorized: false,
      transactionDateUtc: '2026-08-24T00:00:00Z',
      channel: 'CreditCard',
      merchantName: 'Carrefour',
    },
  ],
  generatedAtUtc: '2026-08-27T12:00:00Z',
};

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <DashboardPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('DashboardPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renderiza os KPIs reais vindos do backend', async () => {
    vi.mocked(dashboardApi.getDashboardSummaryApi).mockResolvedValue(mockDashboard);

    renderPage();

    // Antes desta slice, estes valores eram permanentemente R$ 0,00 porque o Gateway
    // nunca enviava receita nem despesa.
    await waitFor(() => {
      expect(screen.getByText(/Entradas do Período/i)).toBeInTheDocument();
    });

    expect(screen.getByText(/Saídas do Período/i)).toBeInTheDocument();
    expect(screen.getByText(/Resultado do Período/i)).toBeInTheDocument();
    expect(screen.getByText(/Superávit no período/i)).toBeInTheDocument();
  });

  it('resolve nome da instituição a partir do institutionId', async () => {
    vi.mocked(dashboardApi.getDashboardSummaryApi).mockResolvedValue(mockDashboard);

    renderPage();

    // O backend envia apenas "itau"; o nome de exibição é resolvido no cliente.
    // Antes, este campo renderizava "undefined".
    await waitFor(() => {
      expect(screen.getByText('Itaú Unibanco')).toBeInTheDocument();
    });

    expect(screen.getByText('Banco Inter')).toBeInTheDocument();
  });

  it('mostra o consumo do limite apenas para contas de cartão', async () => {
    vi.mocked(dashboardApi.getDashboardSummaryApi).mockResolvedValue(mockDashboard);

    renderPage();

    await waitFor(() => {
      // 1117.50 de 4500 = 25%
      expect(screen.getByText('25% do limite')).toBeInTheDocument();
    });

    // A conta corrente não tem barra de limite.
    expect(screen.getAllByRole('progressbar', { name: /limite utilizado/i })).toHaveLength(1);
  });

  it('renderiza as categorias como barras ranqueadas com percentual', async () => {
    vi.mocked(dashboardApi.getDashboardSummaryApi).mockResolvedValue(mockDashboard);

    renderPage();

    await waitFor(() => {
      expect(screen.getByText('Alimentação')).toBeInTheDocument();
    });

    expect(screen.getByText('Transporte')).toBeInTheDocument();
    expect(screen.getByText('59.3%')).toBeInTheDocument();
  });

  it('mostra empty state quando não há dados no período', async () => {
    vi.mocked(dashboardApi.getDashboardSummaryApi).mockResolvedValue({
      ...mockDashboard,
      institutionBalances: [],
      categoryExpenses: [],
      monthlyCashFlow: [],
      recentTransactions: [],
    });

    renderPage();

    await waitFor(() => {
      expect(screen.getByText(/Nenhuma conta sincronizada ainda/i)).toBeInTheDocument();
    });

    expect(screen.getByText(/Sem despesas categorizadas no período/i)).toBeInTheDocument();
    expect(screen.getByText(/Nenhum lançamento no período selecionado/i)).toBeInTheDocument();
  });

  it('exibe erro RFC 7807 com botão de tentar novamente', async () => {
    vi.mocked(dashboardApi.getDashboardSummaryApi).mockRejectedValue(
      new ApiError({
        type: 'about:blank',
        title: 'Falha ao consultar o painel',
        status: 502,
        detail: 'O serviço de agregação está indisponível.',
      }),
    );

    renderPage();

    await waitFor(() => {
      expect(screen.getByText('O serviço de agregação está indisponível.')).toBeInTheDocument();
    });

    expect(screen.getByRole('button', { name: /tentar novamente/i })).toBeInTheDocument();
  });

  it('refaz a consulta ao trocar o preset de período', async () => {
    vi.mocked(dashboardApi.getDashboardSummaryApi).mockResolvedValue(mockDashboard);
    const user = userEvent.setup();

    renderPage();

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'Mês Anterior' })).toBeInTheDocument();
    });

    vi.mocked(dashboardApi.getDashboardSummaryApi).mockClear();
    await user.click(screen.getByRole('button', { name: 'Mês Anterior' }));

    await waitFor(() => {
      expect(dashboardApi.getDashboardSummaryApi).toHaveBeenCalled();
    });

    const [filtersArg] = vi.mocked(dashboardApi.getDashboardSummaryApi).mock.calls[0];
    expect(filtersArg?.datePreset).toBe('previous-month');
    expect(filtersArg?.startDate).toBeDefined();
  });

  it('não pisca skeleton ao trocar de período, mantendo os dados anteriores', async () => {
    vi.mocked(dashboardApi.getDashboardSummaryApi).mockResolvedValue(mockDashboard);
    const user = userEvent.setup();

    renderPage();

    await waitFor(() => {
      expect(screen.getByText('Itaú Unibanco')).toBeInTheDocument();
    });

    await user.click(screen.getByRole('button', { name: 'Ano Atual' }));

    // keepPreviousData mantém a lista montada durante o refetch (Regra 25).
    expect(screen.getByText('Itaú Unibanco')).toBeInTheDocument();
    expect(screen.queryByLabelText(/Carregando visão geral do painel/i)).not.toBeInTheDocument();
  });
});
