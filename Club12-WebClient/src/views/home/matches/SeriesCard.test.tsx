import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it } from 'vitest';
import { GUID } from '@/modules/core/types/types';
import { MatchType } from '@/modules/core/enum/match/matchType';
import { IMatchResponse } from '@/modules/match/type/match';
import { ITeamMatchResponse } from '@/modules/team/type/team';
import { IMatchSeriesResponse } from '@/modules/matchSeries/type/matchSeries.d';
import SeriesCard from '@/views/home/matches/SeriesCard';

const guid = (value: string) => value as GUID;

const team = (name: string, score: number): ITeamMatchResponse => ({
  id: guid(`team-${name}`),
  name,
  logoUrl: '',
  score,
  players: [],
  scorers: [],
});

let sequence = 0;

const game = (overrides: Partial<IMatchResponse>): IMatchResponse => ({
  id: guid(`game-${(sequence += 1)}`),
  matchDate: '2026-05-13T00:00:00Z',
  round: null,
  matchType: MatchType.Playoff,
  slug: `game-${sequence}`,
  homeTeam: team('Sionista', 0),
  visitorTeam: team('Estudiantes', 0),
  isFinished: false,
  winningTeamId: null,
  venue: null,
  stageId: guid('stage-1'),
  winningTeamName: null,
  status: null,
  ...overrides,
});

const series = (overrides: Partial<IMatchSeriesResponse> = {}): IMatchSeriesResponse => ({
  id: guid('series-1'),
  stageId: guid('stage-1'),
  homeTeamId: guid('team-Sionista'),
  homeTeamName: 'Sionista',
  visitorTeamId: guid('team-Estudiantes'),
  visitorTeamName: 'Estudiantes',
  bestOf: 3,
  winningTeamId: null,
  winningTeamName: null,
  games: [],
  ...overrides,
});

const renderCard = (props: Parameters<typeof SeriesCard>[0]) =>
  render(
    <MemoryRouter>
      <SeriesCard {...props} />
    </MemoryRouter>
  );

describe('SeriesCard', () => {
  it('shows the matchup, format and live game count for an undecided series', () => {
    const g1 = game({
      homeTeam: team('Sionista', 61),
      visitorTeam: team('Estudiantes', 73),
      isFinished: true,
      winningTeamName: 'Estudiantes',
    });

    renderCard({ series: series(), matches: [g1] });

    expect(screen.getByText('Sionista vs Estudiantes')).toBeInTheDocument();
    expect(screen.getByText('Al mejor de 3 · 1 juego')).toBeInTheDocument();
    expect(screen.getByText('Serie 0 - 1')).toBeInTheDocument();
    expect(screen.queryByText('Ganador')).not.toBeInTheDocument();
  });

  it('shows the winner once the series is decided', () => {
    const g1 = game({
      homeTeam: team('Sionista', 61),
      visitorTeam: team('Estudiantes', 73),
      isFinished: true,
      winningTeamName: 'Estudiantes',
    });
    const g2 = game({
      homeTeam: team('Sionista', 78),
      visitorTeam: team('Estudiantes', 74),
      isFinished: true,
      winningTeamName: 'Sionista',
    });
    const g3 = game({
      homeTeam: team('Sionista', 83),
      visitorTeam: team('Estudiantes', 78),
      isFinished: true,
      winningTeamName: 'Sionista',
    });

    renderCard({
      series: series({ winningTeamId: guid('team-Sionista'), winningTeamName: 'Sionista' }),
      matches: [g1, g2, g3],
    });

    expect(screen.getByText('Ganador')).toBeInTheDocument();
    expect(screen.getByText('Serie 2 - 1')).toBeInTheDocument();
  });

  it('labels every game "Juego N" in order', () => {
    const g1 = game({ isFinished: true, winningTeamName: 'Sionista' });
    const g2 = game({ isFinished: true, winningTeamName: 'Estudiantes' });

    renderCard({ series: series(), matches: [g1, g2] });

    expect(screen.getByText('Juego 1')).toBeInTheDocument();
    expect(screen.getByText('Juego 2')).toBeInTheDocument();
  });

  it('collapses and reopens the games list without touching the headline result', async () => {
    const user = userEvent.setup();
    const g1 = game({ isFinished: true, winningTeamName: 'Sionista' });

    renderCard({
      series: series({ winningTeamId: guid('team-Sionista'), winningTeamName: 'Sionista' }),
      matches: [g1],
    });

    const toggle = screen.getByRole('button', { name: /ocultar juegos de la serie/i });
    expect(toggle).toHaveAttribute('aria-expanded', 'true');

    await user.click(toggle);
    expect(
      screen.getByRole('button', { name: /mostrar juegos de la serie/i })
    ).toHaveAttribute('aria-expanded', 'false');
    // The headline (matchup, format, winner) stays visible regardless.
    expect(screen.getByText('Sionista vs Estudiantes')).toBeInTheDocument();
    expect(screen.getByText('Ganador')).toBeInTheDocument();
  });
});
