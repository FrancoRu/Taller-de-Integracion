import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import PlayerHistory from '@/views/playerStatistic/PlayerHistory';
import { GUID } from '@/modules/core/types/types';
import { PlayerHistoryResponse } from '@/modules/playerStatistic/type/playerStatistic';

const history: PlayerHistoryResponse = {
  playerId: 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee' as GUID,
  fullName: 'PEREZ Juan',
  seasons: [
    {
      season: 2026,
      tournamentId: '11111111-2222-3333-4444-555555555555' as GUID,
      tournamentName: 'Apertura 2026',
      teamId: '66666666-7777-8888-9999-000000000000' as GUID,
      teamName: 'River',
      totalPoints: 30,
      gamesPlayed: 6,
      sanctions: [
        {
          sanctionId: 'abcabcab-1111-2222-3333-444444444444' as GUID,
          description: 'Doble amarilla',
          duration: 1,
          issuedDate: '2026-05-10T00:00:00Z',
          matchId: 'dddddddd-eeee-ffff-0000-111111111111' as GUID,
        },
      ],
    },
    {
      season: 2025,
      tournamentId: '22222222-3333-4444-5555-666666666666' as GUID,
      tournamentName: 'Clausura 2025',
      teamId: '77777777-8888-9999-0000-111111111111' as GUID,
      teamName: 'Boca',
      totalPoints: 12,
      gamesPlayed: 4,
      sanctions: [],
    },
  ],
};

describe('PlayerHistory', () => {
  it('renders one entry per season with team and stats', () => {
    render(<PlayerHistory history={history} />);

    expect(screen.getByText('2026')).toBeInTheDocument();
    expect(screen.getByText('2025')).toBeInTheDocument();
    expect(screen.getByText(/River · Apertura 2026/)).toBeInTheDocument();
    expect(screen.getByText(/Boca · Clausura 2025/)).toBeInTheDocument();
    expect(screen.getByText('30 pts')).toBeInTheDocument();
    expect(screen.getByText('6 PJ')).toBeInTheDocument();
  });

  it('renders the sanctions of a season, and an empty note when none', () => {
    render(<PlayerHistory history={history} />);

    expect(screen.getByText('Doble amarilla')).toBeInTheDocument();
    expect(screen.getByText('1 fechas')).toBeInTheDocument();
    expect(screen.getByText('Sin sanciones esta temporada.')).toBeInTheDocument();
  });

  it('shows an empty message when there is no history', () => {
    render(<PlayerHistory history={null} />);

    expect(
      screen.getByText(/todavía no tiene historial entre temporadas/i)
    ).toBeInTheDocument();
  });
});
