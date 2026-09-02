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

  it('draws thin pinstripe bars for the pinstripe template', () => {
    const { container } = render(
      <JerseySvg color="#1e5fcc" secondaryColor="#ffffff" style="pinstripe" />
    );
    const bars = container.querySelectorAll('rect[fill="#ffffff"][width="4"]');
    expect(bars.length).toBeGreaterThan(5);
  });

  it('draws a top yoke panel for the yoke template', () => {
    const { container } = render(
      <JerseySvg color="#1e5fcc" secondaryColor="#ffffff" style="yoke" />
    );
    expect(
      container.querySelector('rect[fill="#ffffff"][height="68"]')
    ).not.toBeNull();
  });

  it('draws a diagonal color-block polygon for the colorblock template', () => {
    const { container } = render(
      <JerseySvg color="#1e5fcc" secondaryColor="#ffffff" style="colorblock" />
    );
    expect(container.querySelector('polygon[fill="#ffffff"]')).not.toBeNull();
  });

  it('draws a single chest accent for the arrow template', () => {
    const { container } = render(
      <JerseySvg color="#1e5fcc" secondaryColor="#ffffff" style="arrow" />
    );
    expect(container.querySelectorAll('polygon[fill="#ffffff"]').length).toBe(1);
  });

  it('falls back to a solid body for an unknown style (no pattern shapes)', () => {
    const { container } = render(<JerseySvg color="#1e5fcc" style="bogus" />);
    expect(container.querySelector('polygon')).toBeNull();
    expect(container.querySelector('pattern')).toBeNull();
  });

  it('uses the third color for the triband template when one is set', () => {
    const { container } = render(
      <JerseySvg
        color="#1e5fcc"
        secondaryColor="#ffffff"
        tertiaryColor="#00aa55"
        style="triband"
      />
    );
    expect(container.querySelector('rect[fill="#ffffff"]')).not.toBeNull();
    expect(container.querySelector('rect[fill="#00aa55"]')).not.toBeNull();
  });

  it('falls back to the secondary color for a tri-color template when no third color is set', () => {
    const { container } = render(
      <JerseySvg color="#1e5fcc" secondaryColor="#ffffff" style="triband" />
    );
    // Both bands fall back to secondary — two white bars, no third hue.
    expect(container.querySelectorAll('rect[fill="#ffffff"]').length).toBe(2);
  });

  it('draws an inset border in the third color for the frame template', () => {
    const { container } = render(
      <JerseySvg
        color="#1e5fcc"
        secondaryColor="#ffffff"
        tertiaryColor="#00aa55"
        style="frame"
      />
    );
    expect(container.querySelector('path[stroke="#00aa55"]')).not.toBeNull();
  });

  it('draws a five-point star for the star template', () => {
    const { container } = render(
      <JerseySvg color="#1e5fcc" secondaryColor="#ffffff" style="star" />
    );
    const star = container.querySelector('polygon[fill="#ffffff"]');
    expect(star).not.toBeNull();
    expect(star!.getAttribute('points')!.trim().split(/\s+/)).toHaveLength(10);
  });

  it('defines a checkerboard pattern for the checkerboard template', () => {
    const { container } = render(<JerseySvg color="#1e5fcc" style="checkerboard" />);
    expect(container.querySelectorAll('pattern rect').length).toBeGreaterThan(0);
  });
});
