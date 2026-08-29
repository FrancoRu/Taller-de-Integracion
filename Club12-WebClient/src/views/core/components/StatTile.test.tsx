import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import StatTile from '@/views/core/components/StatTile';

describe('StatTile', () => {
  it('renders the label, value and optional sub-line', () => {
    render(<StatTile label="Posición" value="3º" sub="de 8 · Zona A" />);

    expect(screen.getByText('Posición')).toBeInTheDocument();
    expect(screen.getByText('3º')).toBeInTheDocument();
    expect(screen.getByText('de 8 · Zona A')).toBeInTheDocument();
  });

  it('renders without a sub-line', () => {
    render(<StatTile label="Record" value="5-2" />);

    expect(screen.getByText('Record')).toBeInTheDocument();
    expect(screen.getByText('5-2')).toBeInTheDocument();
  });
});
