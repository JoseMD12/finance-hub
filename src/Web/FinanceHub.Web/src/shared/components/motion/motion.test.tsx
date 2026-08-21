import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import {
  GlowCard,
  LogoMark,
  NavActiveIndicator,
  MagneticButton,
  NumberScramble,
  AuroraBackground,
  ScannerReveal,
} from './index';

describe('Motion Components Suite', () => {
  it('renders GlowCard with children and custom glow rgb', () => {
    render(
      <GlowCard glowRgb="224, 86, 151" data-testid="glow-card">
        <div>Content Inside GlowCard</div>
      </GlowCard>
    );

    expect(screen.getByTestId('glow-card')).toBeInTheDocument();
    expect(screen.getByText('Content Inside GlowCard')).toBeInTheDocument();
  });

  it('renders LogoMark with icon and brand text when showText is true', () => {
    render(<LogoMark size="md" showText />);

    expect(screen.getByText('F')).toBeInTheDocument();
    expect(screen.getByText('Finance')).toBeInTheDocument();
    expect(screen.getByText('Hub')).toBeInTheDocument();
  });

  it('renders NavActiveIndicator with default layoutId', () => {
    const { container } = render(<NavActiveIndicator />);
    expect(container.querySelector('span')).toBeInTheDocument();
  });

  it('renders MagneticButton with child content', () => {
    render(
      <MagneticButton strength={0.3}>
        <button type="button">Click Me</button>
      </MagneticButton>
    );

    expect(screen.getByRole('button', { name: /click me/i })).toBeInTheDocument();
  });

  it('renders NumberScramble with formatted values', () => {
    render(
      <NumberScramble
        value={1500}
        format={(v) => `R$ ${v.toFixed(2)}`}
      />
    );

    // Initial render displays formatted value
    expect(screen.getByText(/R\$/)).toBeInTheDocument();
  });

  it('renders AuroraBackground canvas element', () => {
    const { container } = render(<AuroraBackground />);
    expect(container.querySelector('canvas')).toBeInTheDocument();
  });

  it('renders ScannerReveal as a div container with children', () => {
    render(
      <ScannerReveal>
        <div>Item 1</div>
        <div>Item 2</div>
      </ScannerReveal>
    );

    expect(screen.getByText('Item 1')).toBeInTheDocument();
    expect(screen.getByText('Item 2')).toBeInTheDocument();
  });

  it('renders ScannerReveal as tbody with rows and preserves attributes', () => {
    render(
      <table>
        <ScannerReveal as="tbody" className="custom-tbody">
          <tr className="custom-row-class" data-testid="test-row">
            <td>Row 1</td>
          </tr>
        </ScannerReveal>
      </table>
    );

    const row = screen.getByTestId('test-row');
    expect(row).toBeInTheDocument();
    expect(row).toHaveClass('custom-row-class');
    expect(row).toHaveClass('scanner-item');
    expect(screen.getByText('Row 1')).toBeInTheDocument();
  });

  it('renders ScannerReveal as ul with items', () => {
    render(
      <ScannerReveal as="ul" className="custom-ul">
        <li data-testid="list-item">List Item 1</li>
      </ScannerReveal>
    );

    const item = screen.getByTestId('list-item');
    expect(item).toBeInTheDocument();
    expect(item).toHaveClass('scanner-item');
    expect(screen.getByText('List Item 1')).toBeInTheDocument();
  });
});
