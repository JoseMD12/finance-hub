import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { PageContainer } from './PageContainer';

describe('PageContainer Component', () => {
  it('renders children correctly', () => {
    render(
      <PageContainer>
        <div data-testid="child-content">Conteúdo da página</div>
      </PageContainer>
    );

    expect(screen.getByTestId('child-content')).toHaveTextContent('Conteúdo da página');
  });

  it('renders title and description when provided', () => {
    render(
      <PageContainer
        title="Extrato de Transações"
        description="Controle de fluxo de caixa e categorização inteligente"
      >
        <div>Corpo</div>
      </PageContainer>
    );

    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Extrato de Transações');
    expect(screen.getByText('Controle de fluxo de caixa e categorização inteligente')).toBeInTheDocument();
  });

  it('renders actions slot when provided', () => {
    render(
      <PageContainer
        title="Conexões"
        actions={<button type="button">Nova Conexão</button>}
      >
        <div>Corpo</div>
      </PageContainer>
    );

    expect(screen.getByRole('button', { name: 'Nova Conexão' })).toBeInTheDocument();
  });

  it('allows text selection by not applying select-none to the container', () => {
    const { container } = render(
      <PageContainer title="Teste">
        <p>Texto selecionável</p>
      </PageContainer>
    );

    expect(container.firstChild).not.toHaveClass('select-none');
  });
});
