import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it } from 'vitest';
import { GUID } from '@/modules/core/types/types';
import { MatchType } from '@/modules/core/enum/match/matchType';
import { IMatchResponse } from '@/modules/match/type/match';
import { ITeamMatchResponse } from '@/modules/team/type/team';
import MatchFixtureList from '@/views/home/matches/MatchFixtureList';
import { buildFixtureCsvRows } from '@/modules/match/utils/matchFixtureCsv';

const guid = (value: string) => value as GUID;

const team = (name: string): ITeamMatchResponse => ({
  id: guid(`team-${name}`),
  name,
  logoUrl: '',
  score: 0,
  players: [],
  scorers: [],
});

let sequence = 0;

const match = (overrides: Partial<IMatchResponse>): IMatchResponse => ({
  id: guid(`match-${(sequence += 1)}`),
  matchDate: '2026-04-28T20:00:00Z',
  round: 1,
  matchType: MatchType.Regular,
  slug: `match-${sequence}`,
  homeTeam: team('A'),
  visitorTeam: team('B'),
  isFinished: false,
  winningTeamId: null,
  venue: null,
  stageId: guid('stage-1'),
  winningTeamName: null,
  status: null,
  ...overrides,
});

const renderFixture = (matches: IMatchResponse[]) =>
  render(
    <MemoryRouter>
      <MatchFixtureList matches={matches} />
    </MemoryRouter>
  );

describe('MatchFixtureList', () => {
  it('groups matches under jornada headers ("Fecha N"), not calendar-date headers', () => {
    renderFixture([
      match({ round: 1, homeTeam: team('A'), visitorTeam: team('B') }),
      match({ round: 2, homeTeam: team('A'), visitorTeam: team('B') }),
    ]);

    expect(screen.getByText('Fecha 1')).toBeInTheDocument();
    expect(screen.getByText('Fecha 2')).toBeInTheDocument();
    // The full calendar date must not be used as a group header (HU-63).
    expect(screen.queryByText(/28 de abril/i)).not.toBeInTheDocument();
    expect(screen.queryByText('28/04/2026')).not.toBeInTheDocument();
  });

  it('renders the team with no match that round as "Libre" (HU-65)', () => {
    renderFixture([
      match({ round: 1, homeTeam: team('A'), visitorTeam: team('B') }),
      match({ round: 2, homeTeam: team('C'), visitorTeam: team('A') }),
    ]);

    // Stage roster is A, B, C. Round 1 pairs A vs B (C sits out); round 2 pairs
    // C vs A (B sits out) — one "Libre" per round.
    expect(screen.getAllByText('Libre')).toHaveLength(2);
  });

  it('shows the CSV export button only when an exportTitle is given (HU-89)', () => {
    const { rerender } = render(
      <MemoryRouter>
        <MatchFixtureList
          matches={[match({ round: 1 })]}
        />
      </MemoryRouter>
    );
    expect(
      screen.queryByRole('button', { name: /exportar csv/i })
    ).not.toBeInTheDocument();

    rerender(
      <MemoryRouter>
        <MatchFixtureList matches={[match({ round: 1 })]} exportTitle="Zona A" />
      </MemoryRouter>
    );
    expect(
      screen.getByRole('button', { name: /exportar csv/i })
    ).toBeInTheDocument();
  });
});

describe('buildFixtureCsvRows (HU-89)', () => {
  it('produces one row per match, grouped by round with score and status', () => {
    const rows = buildFixtureCsvRows([
      match({
        round: 2,
        homeTeam: { ...team('C'), score: 1 },
        visitorTeam: { ...team('D'), score: 3 },
        isFinished: true,
      }),
      match({
        round: 1,
        homeTeam: team('A'),
        visitorTeam: team('B'),
        isFinished: false,
      }),
    ]);

    // Rounds are ordered ascending regardless of input order.
    expect(rows[0][0]).toBe('Fecha 1');
    expect(rows[0][2]).toBe('A');
    expect(rows[0][3]).toBe('B');
    expect(rows[0][4]).toBe('—'); // not finished → no score
    expect(rows[0][5]).toBe('Programado');

    expect(rows[1][0]).toBe('Fecha 2');
    expect(rows[1][4]).toBe('1-3');
    expect(rows[1][5]).toBe('Finalizado');
  });
});
