import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import ScorerScopeToggle from '@/views/scorer/ScorerScopeToggle';

const baseProps = {
  scope: 'tournament' as const,
  onScopeChange: vi.fn(),
  season: '' as number | '',
  onSeasonChange: vi.fn(),
  seasonOptions: [2026, 2025],
};

describe('ScorerScopeToggle', () => {
  it('renders the three HU-85 scopes', () => {
    render(<ScorerScopeToggle {...baseProps} onScopeChange={vi.fn()} />);

    expect(
      screen.getByRole('button', { name: 'Por torneo' })
    ).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: 'Por temporada' })
    ).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: 'Histórico (todos los tiempos)' })
    ).toBeInTheDocument();
  });

  it('fires onScopeChange when another scope is selected', () => {
    const onScopeChange = vi.fn();
    render(<ScorerScopeToggle {...baseProps} onScopeChange={onScopeChange} />);

    fireEvent.click(screen.getByRole('button', { name: 'Por temporada' }));

    expect(onScopeChange).toHaveBeenCalledWith('season');
  });

  it('shows the year picker only while the season scope is active', () => {
    const { rerender } = render(
      <ScorerScopeToggle {...baseProps} onScopeChange={vi.fn()} />
    );

    expect(
      screen.queryByRole('combobox', { name: 'Temporada' })
    ).not.toBeInTheDocument();

    rerender(
      <ScorerScopeToggle {...baseProps} scope="season" onScopeChange={vi.fn()} />
    );

    expect(
      screen.getByRole('combobox', { name: 'Temporada' })
    ).toBeInTheDocument();
  });
});
