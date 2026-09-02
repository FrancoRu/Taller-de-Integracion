import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it } from 'vitest';
import { GUID } from '@/modules/core/types/types';
import { MatchType } from '@/modules/core/enum/match/matchType';
import { IMatchResponse } from '@/modules/match/type/match';
import { ITeamMatchResponse } from '@/modules/team/type/team';
import { IMatchSeriesResponse, ISeriesGameResponse } from '@/modules/matchSeries/type/matchSeries.d';
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
    // The full calendar date must not be used as a group header (HU-63) —
    // it legitimately appears inside each match row instead (MatchRow).
    expect(screen.queryByText(/28 de abril/i)).not.toBeInTheDocument();
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

  it('shows no generic "Fase final" header for a knockout stage (null round) — the caller already labels the stage', () => {
    renderFixture([
      match({ round: null, homeTeam: team('A'), visitorTeam: team('B') }),
    ]);

    expect(screen.queryByText('Fase final')).not.toBeInTheDocument();
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

describe('MatchFixtureList — collapsible fechas (all but the current one)', () => {
  it('collapses a past fecha by default but keeps the current/nearest one always expanded, with no toggle', async () => {
    const user = userEvent.setup();
    renderFixture([
      match({
        round: 1,
        matchDate: '2020-01-01T20:00:00Z',
        homeTeam: team('A'),
        visitorTeam: team('B'),
      }),
      match({
        round: 2,
        matchDate: '2099-01-01T20:00:00Z',
        homeTeam: team('C'),
        visitorTeam: team('D'),
      }),
    ]);

    // Fecha 2 is the nearest to "now" (its match is in the future) — always
    // expanded, no toggle button for it.
    const fecha2Row = screen.getByText('Fecha 2').closest('div');
    expect(fecha2Row?.querySelector('button')).toBeNull();
    expect(screen.getAllByText('C').length).toBeGreaterThan(0);

    // Fecha 1 is in the past — collapsed by default, with a toggle.
    const fecha1Toggle = screen.getByRole('button', { name: /Fecha 1/i });
    expect(fecha1Toggle).toHaveAttribute('aria-expanded', 'false');

    await user.click(fecha1Toggle);
    expect(fecha1Toggle).toHaveAttribute('aria-expanded', 'true');
  });
});

describe('MatchFixtureList — playoff series grouping', () => {
  const asGame = (m: IMatchResponse, gameNumber: number): ISeriesGameResponse => ({
    id: m.id,
    matchDate: m.matchDate,
    homeTeamName: m.homeTeam!.name,
    visitorTeamName: m.visitorTeam!.name,
    homeScore: m.homeTeam!.score,
    visitorScore: m.visitorTeam!.score,
    winningTeamName: m.winningTeamName,
    isFinished: m.isFinished,
    matchType: m.matchType,
    gameNumber,
  });

  it("groups a series' games under one shared header instead of listing them as unrelated rows", () => {
    const sionista = team('Sionista');
    const estudiantes = team('Estudiantes');
    const g1 = match({ round: null, homeTeam: sionista, visitorTeam: estudiantes, isFinished: true });
    const g2 = match({ round: null, homeTeam: sionista, visitorTeam: estudiantes, isFinished: true });

    const series: IMatchSeriesResponse = {
      id: guid('series-1'),
      stageId: guid('stage-1'),
      homeTeamId: sionista.id,
      homeTeamName: 'Sionista',
      visitorTeamId: estudiantes.id,
      visitorTeamName: 'Estudiantes',
      bestOf: 3,
      winningTeamId: sionista.id,
      winningTeamName: 'Sionista',
      games: [asGame(g1, 1), asGame(g2, 2)],
    };

    render(
      <MemoryRouter>
        <MatchFixtureList
          matches={[g1, g2]}
          seriesById={new Map([[series.id, series]])}
        />
      </MemoryRouter>
    );

    expect(
      screen.getByText('Serie: Sionista vs Estudiantes · Al mejor de 3 · Ganó Sionista')
    ).toBeInTheDocument();
  });

  it('keeps two different series under the same stage in separate groups, even interleaved by date', () => {
    const sionista = team('Sionista');
    const estudiantes = team('Estudiantes');
    const rocamora = team('Rocamora');
    const olimpia = team('Olimpia');

    const a1 = match({ round: null, matchDate: '2026-05-13T00:00:00Z', homeTeam: sionista, visitorTeam: estudiantes });
    const b1 = match({ round: null, matchDate: '2026-05-14T00:00:00Z', homeTeam: rocamora, visitorTeam: olimpia });
    const a2 = match({ round: null, matchDate: '2026-05-15T00:00:00Z', homeTeam: sionista, visitorTeam: estudiantes });

    const seriesA: IMatchSeriesResponse = {
      id: guid('series-a'),
      stageId: guid('stage-1'),
      homeTeamId: sionista.id,
      homeTeamName: 'Sionista',
      visitorTeamId: estudiantes.id,
      visitorTeamName: 'Estudiantes',
      bestOf: 3,
      winningTeamId: null,
      winningTeamName: null,
      games: [asGame(a1, 1), asGame(a2, 2)],
    };
    const seriesB: IMatchSeriesResponse = {
      id: guid('series-b'),
      stageId: guid('stage-1'),
      homeTeamId: rocamora.id,
      homeTeamName: 'Rocamora',
      visitorTeamId: olimpia.id,
      visitorTeamName: 'Olimpia',
      bestOf: 3,
      winningTeamId: null,
      winningTeamName: null,
      games: [asGame(b1, 1)],
    };

    render(
      <MemoryRouter>
        <MatchFixtureList
          matches={[a1, b1, a2]}
          seriesById={
            new Map([
              [seriesA.id, seriesA],
              [seriesB.id, seriesB],
            ])
          }
        />
      </MemoryRouter>
    );

    expect(
      screen.getByText('Serie: Sionista vs Estudiantes · Al mejor de 3')
    ).toBeInTheDocument();
    expect(
      screen.getByText('Serie: Rocamora vs Olimpia · Al mejor de 3')
    ).toBeInTheDocument();
  });

  it('renders a standalone (non-series) match with no series header, same as without seriesById', () => {
    const single = match({ round: null, homeTeam: team('A'), visitorTeam: team('B') });

    render(
      <MemoryRouter>
        <MatchFixtureList matches={[single]} seriesById={new Map()} />
      </MemoryRouter>
    );

    expect(screen.queryByText(/^Serie:/)).not.toBeInTheDocument();
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
