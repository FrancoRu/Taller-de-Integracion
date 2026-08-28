import { render, screen, within } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import PlayerStatisticCard from '@/views/playerStatistic/PlayerStatisticCard';
import { GUID } from '@/modules/core/types/types';
import { PlayerStatisticCardResponse } from '@/modules/playerStatistic/type/playerStatistic';

const card: PlayerStatisticCardResponse = {
  playerId: 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee' as GUID,
  fullName: 'PEREZ Juan',
  totalPoints: 42,
  gamesPlayed: 10,
  averagePoints: 4.2,
  seasons: [
    { season: 2026, totalPoints: 30, gamesPlayed: 6, averagePoints: 5 },
    { season: 2025, totalPoints: 12, gamesPlayed: 4, averagePoints: 3 },
  ],
};

describe('PlayerStatisticCard', () => {
  it('renders the overall totals and average', () => {
    render(<PlayerStatisticCard card={card} />);

    expect(screen.getByText('42')).toBeInTheDocument();
    expect(screen.getByText('4.2')).toBeInTheDocument();
    expect(screen.getByText('Puntos totales')).toBeInTheDocument();
    expect(screen.getByText('Partidos jugados')).toBeInTheDocument();
    // "Promedio" is both the overall tile label and the per-season column head.
    expect(screen.getAllByText('Promedio').length).toBeGreaterThanOrEqual(1);
  });

  it('renders one row per season with its stats', () => {
    render(<PlayerStatisticCard card={card} />);

    const table = screen.getByRole('table', {
      name: 'Estadísticas por temporada',
    });
    expect(within(table).getByText('2026')).toBeInTheDocument();
    expect(within(table).getByText('2025')).toBeInTheDocument();

    const rows = within(table).getAllByRole('row');
    // header + 2 season rows
    expect(rows).toHaveLength(3);
  });

  it('shows an empty message when there is no card', () => {
    render(<PlayerStatisticCard card={null} />);

    expect(
      screen.getByText(/todavía no tiene estadísticas registradas/i)
    ).toBeInTheDocument();
  });
});
