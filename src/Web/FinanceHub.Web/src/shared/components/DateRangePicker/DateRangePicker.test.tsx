import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import { DateRangePicker } from './DateRangePicker';

describe('DateRangePicker', () => {
  it('deve renderizar o botão trigger e abrir o popover ao clicar', async () => {
    const user = userEvent.setup();
    const handleApply = vi.fn();

    render(
      <DateRangePicker
        onApply={handleApply}
        startDate="2026-08-01"
        endDate="2026-08-25"
        isActive={true}
      />
    );

    const triggerButton = screen.getByRole('button', { name: /01\/08\/2026 - 25\/08\/2026/i });
    expect(triggerButton).toBeInTheDocument();

    await user.click(triggerButton);

    expect(screen.getByText('Selecione o Período')).toBeInTheDocument();
    expect(screen.getByLabelText('Data inicial')).toBeInTheDocument();
    expect(screen.getByLabelText('Data final')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Aplicar' })).toBeInTheDocument();
  });

  it('deve permitir digitar datas manualmente nos inputs sem abrir popup nativo e aplicar', async () => {
    const user = userEvent.setup();
    const handleApply = vi.fn();

    render(<DateRangePicker onApply={handleApply} />);

    const triggerButton = screen.getByRole('button', { name: /personalizado/i });
    await user.click(triggerButton);

    const startInput = screen.getByLabelText('Data inicial') as HTMLInputElement;
    const endInput = screen.getByLabelText('Data final') as HTMLInputElement;

    await user.type(startInput, '10082026');
    expect(startInput.value).toBe('10/08/2026');

    await user.type(endInput, '20082026');
    expect(endInput.value).toBe('20/08/2026');

    const applyButton = screen.getByRole('button', { name: 'Aplicar' });
    await user.click(applyButton);

    expect(handleApply).toHaveBeenCalledWith({
      startDate: '2026-08-10',
      endDate: '2026-08-20',
    });
  });

  it('deve sincronizar os inputs de texto ao clicar nos dias do calendário', async () => {
    const user = userEvent.setup();
    const handleApply = vi.fn();

    render(
      <DateRangePicker
        onApply={handleApply}
        startDate="2026-08-01"
        endDate="2026-08-05"
        isActive={true}
      />
    );

    const triggerButton = screen.getByRole('button', { name: /01\/08\/2026 - 05\/08\/2026/i });
    await user.click(triggerButton);

    const startInput = screen.getByLabelText('Data inicial') as HTMLInputElement;
    const endInput = screen.getByLabelText('Data final') as HTMLInputElement;

    // Clicar no dia 15 como início
    const day15 = screen.getByRole('button', { name: '15' });
    await user.click(day15);
    expect(startInput.value).toBe('15/08/2026');
    expect(endInput.value).toBe('');

    // Clicar no dia 22 como fim
    const day22 = screen.getByRole('button', { name: '22' });
    await user.click(day22);
    expect(endInput.value).toBe('22/08/2026');

    // Aplicar
    const applyButton = screen.getByRole('button', { name: 'Aplicar' });
    await user.click(applyButton);

    expect(handleApply).toHaveBeenCalledWith({
      startDate: '2026-08-15',
      endDate: '2026-08-22',
    });
  });

  it('não deve destacar nem exibir datas no botão quando isActive for falso (preset rápido ativo)', () => {
    const handleApply = vi.fn();

    const { rerender } = render(
      <DateRangePicker
        onApply={handleApply}
        startDate="2026-08-01"
        endDate="2026-08-31"
        isActive={false}
      />
    );

    const button = screen.getByRole('button', { name: 'Personalizado' });
    expect(button).toBeInTheDocument();
    expect(button).toHaveAttribute('aria-pressed', 'false');

    // Quando o usuário define o filtro como personalizado (isActive = true)
    rerender(
      <DateRangePicker
        onApply={handleApply}
        startDate="2026-08-10"
        endDate="2026-08-20"
        isActive={true}
      />
    );

    const activeBtn = screen.getByRole('button', { name: '10/08/2026 - 20/08/2026' });
    expect(activeBtn).toBeInTheDocument();
    expect(activeBtn).toHaveAttribute('aria-pressed', 'true');
  });
});
