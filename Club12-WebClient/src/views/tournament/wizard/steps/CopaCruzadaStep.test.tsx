import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import CopaCruzadaStep from './CopaCruzadaStep';
import { CrossCupConfig } from '../types';

const makeCrossCup = (overrides: Partial<CrossCupConfig> = {}): CrossCupConfig => ({
  enabled: true,
  name: 'Copa Club12',
  groupCount: 2,
  qualifiersPerGroup: 1,
  roundRobinLegs: 1,
  cups: [],
  pointsForWin: 2,
  pointsForLoss: 1,
  playoffMappings: [],
  ...overrides,
});

describe('CopaCruzadaStep — multi-group cross cup (HU-110)', () => {
  it('renders the group-count and qualifiers-per-group fields when enabled', () => {
    render(<CopaCruzadaStep value={makeCrossCup()} onChange={vi.fn()} />);

    expect(screen.getByLabelText(/cantidad de grupos/i)).toHaveValue(2);
    expect(screen.getByLabelText(/clasifican por grupo/i)).toHaveValue(1);
  });

  it('does not render the group fields while the cross cup is disabled', () => {
    render(
      <CopaCruzadaStep value={makeCrossCup({ enabled: false })} onChange={vi.fn()} />
    );

    expect(screen.queryByLabelText(/cantidad de grupos/i)).not.toBeInTheDocument();
    expect(screen.queryByLabelText(/clasifican por grupo/i)).not.toBeInTheDocument();
  });

  it('reports a new group count through onChange', () => {
    const onChange = vi.fn();
    render(<CopaCruzadaStep value={makeCrossCup()} onChange={onChange} />);

    fireEvent.change(screen.getByLabelText(/cantidad de grupos/i), {
      target: { value: '4' },
    });

    expect(onChange).toHaveBeenCalledWith(
      expect.objectContaining({ groupCount: 4 })
    );
  });

  it('reports a new qualifiers-per-group value through onChange', () => {
    const onChange = vi.fn();
    render(<CopaCruzadaStep value={makeCrossCup()} onChange={onChange} />);

    fireEvent.change(screen.getByLabelText(/clasifican por grupo/i), {
      target: { value: '3' },
    });

    expect(onChange).toHaveBeenCalledWith(
      expect.objectContaining({ qualifiersPerGroup: 3 })
    );
  });
});
