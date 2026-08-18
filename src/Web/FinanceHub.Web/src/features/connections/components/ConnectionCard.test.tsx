import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { ConnectionCard } from './ConnectionCard';
import type { PluggyItemDto } from '../types/connections.types';

describe('ConnectionCard Component', () => {
  const mockPositiveItem: PluggyItemDto = {
    id: 'item-12345678',
    status: 'UPDATED',
    connector: {
      id: 1,
      name: 'Itaú Unibanco',
    },
    totalBalance: 12500.75,
    accountsCount: 2,
    totalCredit: 300,
    lastUpdatedAt: '2026-08-18T18:30:00Z',
  };

  const mockNegativeItem: PluggyItemDto = {
    id: 'item-87654321',
    status: 'UPDATED',
    connector: {
      id: 2,
      name: 'Banco Inter',
    },
    totalBalance: -350.20,
    accountsCount: 1,
    totalCredit: 0,
  };

  it('renders institution name, status, and positive formatted balance', () => {
    render(<ConnectionCard item={mockPositiveItem} />);

    expect(screen.getByText('Itaú Unibanco')).toBeInTheDocument();
    expect(screen.queryByText('Conectado')).not.toBeInTheDocument();
    const balanceEl = screen.getByText('R$ 12.500,75');
    expect(balanceEl).toBeInTheDocument();
    expect(balanceEl).not.toHaveClass('text-status-danger');
    expect(screen.getByText('Crédito Total')).toBeInTheDocument();
    expect(screen.getByText('R$ 300,00')).toBeInTheDocument();
    expect(screen.getByText('Atualizado: 18/08/2026, 15:30')).toBeInTheDocument();
  });

  it('renders negative balance with negative danger color highlight', () => {
    render(<ConnectionCard item={mockNegativeItem} />);

    expect(screen.getByText('Banco Inter')).toBeInTheDocument();
    const balanceEl = screen.getByText('-R$ 350,20');
    expect(balanceEl).toBeInTheDocument();
    expect(balanceEl).toHaveClass('text-status-danger');
  });

  it('hides the update button when application credentials are unavailable', () => {
    render(<ConnectionCard item={mockPositiveItem} />);

    expect(screen.queryByRole('button', { name: 'Ressincronizar Itaú Unibanco' })).not.toBeInTheDocument();
  });

});
