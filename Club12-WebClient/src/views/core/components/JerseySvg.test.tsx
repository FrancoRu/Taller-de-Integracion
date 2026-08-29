import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import JerseySvg from './JerseySvg';

describe('JerseySvg', () => {
  it('renders an accessible image with a default label', () => {
    render(<JerseySvg color="#ff0000" />);
    expect(screen.getByRole('img', { name: 'Camiseta del equipo' })).toBeInTheDocument();
  });

  it('uses a custom title as the accessible label', () => {
    render(<JerseySvg color="#ff0000" title="Racing — camiseta" />);
    expect(screen.getByRole('img', { name: 'Racing — camiseta' })).toBeInTheDocument();
  });

  it('prints the dorsal number when provided', () => {
    render(<JerseySvg color="#ffffff" number={23} />);
    expect(screen.getByText('23')).toBeInTheDocument();
  });

  it('draws the striped pattern bars for the stripes template', () => {
    const { container } = render(
      <JerseySvg color="#1e5fcc" secondaryColor="#ffffff" style="stripes" />
    );
    // five secondary bars clipped to the body
    expect(container.querySelectorAll('rect[fill="#ffffff"]').length).toBe(5);
  });

  it('defines a linear gradient for the gradient template', () => {
    const { container } = render(
      <JerseySvg color="#1e5fcc" secondaryColor="#111111" style="gradient" />
    );
    expect(container.querySelector('linearGradient')).not.toBeNull();
  });

  it('defines a dot pattern for the circles template', () => {
    const { container } = render(<JerseySvg color="#1e5fcc" style="circles" />);
    expect(container.querySelector('pattern circle')).not.toBeNull();
  });

  it('falls back to a solid body for an unknown style (no pattern shapes)', () => {
    const { container } = render(<JerseySvg color="#1e5fcc" style="bogus" />);
    expect(container.querySelector('polygon')).toBeNull();
    expect(container.querySelector('pattern')).toBeNull();
  });
});
