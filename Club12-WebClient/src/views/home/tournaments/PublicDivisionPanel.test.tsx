import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { GUID } from '@/modules/core/types/types';
import {
  GroupStandings,
  IDivisionResponse,
  Position,
} from '@/modules/division/type/division.d';
import { IStageResponse, StageType } from '@/modules/stage/type/stage';
import { IMatchResponse } from '@/modules/match/type/match.d';
import { MatchType } from '@/modules/core/enum/match/matchType';
import PublicDivisionPanel from '@/views/home/tournaments/PublicDivisionPanel';

const getStagesByFilters = vi.fn().mockResolvedValue({ data: { items: [] } });
const getMatchByFilter = vi.fn().mockResolvedValue({ data: { items: [] } });

// The POSICIONES tab renders synchronously from the division prop, but child
// components (PrintableResultsSheet) and the panel's other tabs lazily hit
// these services. Stub them so a bare render never makes a real request.
vi.mock('@/modules/scorer/service/scorer.service', () => ({
  scorerService: {
    getScorersByPlayerFiltered: vi.fn().mockResolvedValue({ data: { items: [] } }),
  },
}));
vi.mock('@/modules/stage/service/stage.service', () => ({
  stageService: { getStagesByFilters: (...args: unknown[]) => getStagesByFilters(...args) },
}));
vi.mock('@/modules/match/service/match.service', () => ({
  matchService: { getMatchByFilter: (...args: unknown[]) => getMatchByFilter(...args) },
}));
vi.mock('@/modules/matchSeries/service/matchSeries.service', () => ({
  matchSeriesService: {
    getMatchSeriesByFilters: vi.fn().mockResolvedValue({ data: { items: [] } }),
  },
}));

const guid = (value: string) => value as GUID;

const position = (teamId: string, teamName: string): Position => ({
  teamId: guid(teamId),
  teamName,
  logoUrl: '',
  matchesPlayed: 1,
  wins: 1,
  losses: 0,
  pointsFor: 90,
  pointsAgainst: 80,
  pointsDifference: 10,
  points: 2,
});

const group = (stageId: string, stageName: string, positions: Position[]): GroupStandings => ({
  stageId: guid(stageId),
  stageName,
  positions,
});

const division = (overrides: Partial<IDivisionResponse>): IDivisionResponse => ({
  id: guid('division-1'),
  name: 'Copa Club 12',
  slug: 'copa-club-12',
  isFinished: false,
  tournamentId: guid('tournament-1'),
  isCrossDivisionCup: true,
  ...overrides,
});

const renderPanel = (value: IDivisionResponse) =>
  render(
    <MemoryRouter>
      <PublicDivisionPanel division={value} teams={[]} />
    </MemoryRouter>
  );

describe('PublicDivisionPanel — POSICIONES (HU-110)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders one labelled standings table per group for a multi-group cross cup', () => {
    renderPanel(
      division({
        groupStandings: [
          group('stage-g1', 'Grupo 1', [
            position('team-a', 'Equipo A'),
            position('team-b', 'Equipo B'),
          ]),
          group('stage-g2', 'Grupo 2', [
            position('team-c', 'Equipo C'),
            position('team-d', 'Equipo D'),
          ]),
        ],
      })
    );

    // One subheader per group, labelled by the group stage name.
    expect(screen.getByRole('heading', { name: 'Grupo 1' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Grupo 2' })).toBeInTheDocument();

    // Every group's teams are shown — not just one group's.
    expect(screen.getByText('Equipo A')).toBeInTheDocument();
    expect(screen.getByText('Equipo B')).toBeInTheDocument();
    expect(screen.getByText('Equipo C')).toBeInTheDocument();
    expect(screen.getByText('Equipo D')).toBeInTheDocument();

    // Two separate standings tables, one per group.
    expect(screen.getAllByRole('table')).toHaveLength(2);
  });

  it('renders a single table (no group subheaders) for a regular one-group zone', () => {
    renderPanel(
      division({
        name: 'Zona A',
        isCrossDivisionCup: false,
        positions: [position('team-a', 'Equipo A'), position('team-b', 'Equipo B')],
        groupStandings: [
          group('stage-g1', 'Fase de Grupos', [
            position('team-a', 'Equipo A'),
            position('team-b', 'Equipo B'),
          ]),
        ],
      })
    );

    expect(screen.getAllByText('Equipo A').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Equipo B').length).toBeGreaterThan(0);
    // Single-group zones keep the single-table layout: no per-group subheader.
    expect(screen.queryByRole('heading', { name: 'Grupo 1' })).not.toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Fase de Grupos' })).not.toBeInTheDocument();
  });
});

describe('PublicDivisionPanel — Partidos vs Playoff split', () => {
  const groupStage: IStageResponse = {
    id: guid('stage-group'),
    name: 'Zona A - Fase de Grupos',
    slug: 'zona-a-fase-de-grupos',
    stageType: StageType.Group,
    isActive: true,
    isElimination: false,
    startDate: '2026-01-01T00:00:00Z',
    endDate: '2026-02-01T00:00:00Z',
    divisionId: guid('division-1'),
    order: 0,
    bestOf: 1,
    roundRobinLegs: 1,
  };

  const koStage: IStageResponse = {
    id: guid('stage-final'),
    name: 'Zona A - Final',
    slug: 'zona-a-final',
    stageType: StageType.Final,
    isActive: true,
    isElimination: true,
    startDate: '2026-02-01T00:00:00Z',
    endDate: '2026-02-08T00:00:00Z',
    divisionId: guid('division-1'),
    order: 1,
    bestOf: 1,
    roundRobinLegs: 1,
  };

  const buildMatch = (id: string, stageId: GUID, opponentName: string): IMatchResponse => ({
    id: guid(id),
    matchDate: '2026-01-15T20:00:00Z',
    matchType: MatchType.Regular,
    slug: id,
    homeTeam: {
      id: guid('team-home'),
      name: 'Equipo Local',
      logoUrl: '',
      score: 0,
      players: [],
      scorers: [],
    },
    visitorTeam: {
      id: guid('team-away'),
      name: opponentName,
      logoUrl: '',
      score: 0,
      players: [],
      scorers: [],
    },
    isFinished: false,
    winningTeamId: null,
    winningTeamName: null,
    venue: null,
    stageId,
  });

  beforeEach(() => {
    getStagesByFilters.mockResolvedValue({ data: { items: [groupStage, koStage] } });
    // Regular-season and playoff matches are now fetched as two separate,
    // type-scoped calls — the mock must respond per call the same way the
    // real API does, or both calls return the same combined list and every
    // match ends up duplicated in the panel's state.
    getMatchByFilter.mockImplementation((filter: { type?: MatchType }) => {
      if (filter.type === MatchType.Playoff) {
        return Promise.resolve({
          data: { items: [buildMatch('match-final', koStage.id, 'Rival de la final')] },
        });
      }
      return Promise.resolve({
        data: { items: [buildMatch('match-group', groupStage.id, 'Rival de grupos')] },
      });
    });
  });

  it('shows the group-stage match under Partidos but not the playoff match', async () => {
    render(
      <MemoryRouter initialEntries={['/?view=partidos']}>
        <PublicDivisionPanel division={division({ name: 'Zona A' })} teams={[]} />
      </MemoryRouter>
    );

    await waitFor(() => expect(screen.getByText('Rival de grupos')).toBeInTheDocument());
    expect(screen.queryByText('Rival de la final')).not.toBeInTheDocument();
  });

  it('renames the bracket tab to "Playoff"', async () => {
    render(
      <MemoryRouter>
        <PublicDivisionPanel division={division({ name: 'Zona A' })} teams={[]} />
      </MemoryRouter>
    );

    // The tab is now gated behind the eager stages fetch (see the "no
    // playoff" describe block below), so it only appears once that
    // resolves and confirms an elimination stage actually exists.
    await waitFor(() => expect(screen.getByRole('tab', { name: 'Playoff' })).toBeInTheDocument());
    expect(screen.queryByRole('tab', { name: 'Llaves' })).not.toBeInTheDocument();
  });

  it('shows a real match list alongside the bracket on the Playoff tab', async () => {
    render(
      <MemoryRouter initialEntries={['/?view=playoff']}>
        <PublicDivisionPanel division={division({ name: 'Zona A' })} teams={[]} />
      </MemoryRouter>
    );

    await waitFor(() =>
      expect(screen.getByRole('heading', { name: 'Partidos' })).toBeInTheDocument()
    );
    // The playoff match (excluded from "Partidos") shows up here as a real
    // fixture row, not just inside the collapsed bracket card.
    expect(screen.getAllByText('Rival de la final').length).toBeGreaterThan(0);
  });

  it('explains where the matches are, instead of a generic empty state, when the division is groupless', async () => {
    // A playoffs-only division has no Group-type stage at all — "Partidos"
    // is empty by design, not because nothing was ever scheduled. The
    // generic "no hay partidos" message reads as "nothing exists anywhere",
    // which sent an admin looking for a fixture that was never going to be
    // here (its matches are on the Playoff tab).
    getStagesByFilters.mockResolvedValue({ data: { items: [koStage] } });
    getMatchByFilter.mockImplementation((filter: { type?: MatchType }) => {
      if (filter.type === MatchType.Playoff) {
        return Promise.resolve({
          data: { items: [buildMatch('match-final', koStage.id, 'Rival de la final')] },
        });
      }
      return Promise.resolve({ data: { items: [] } });
    });

    render(
      <MemoryRouter initialEntries={['/?view=partidos']}>
        <PublicDivisionPanel division={division({ name: 'Zona A' })} teams={[]} />
      </MemoryRouter>
    );

    // The clarifying message only appears once the lazy structure fetch
    // (gated on the eager stages fetch resolving first) has actually run —
    // wait for that explicitly rather than relying on findByText's own
    // polling to catch a multi-effect async chain in one pass.
    await waitFor(() => expect(getMatchByFilter).toHaveBeenCalled());

    expect(
      await screen.findByText(/sus partidos están en la pestaña Playoff/i)
    ).toBeInTheDocument();
    expect(screen.queryByText(/^No hay partidos registrados/)).not.toBeInTheDocument();
  });
});

describe('PublicDivisionPanel — division with no playoff phase', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    // No elimination stage at all — a "liga sola" division decided by
    // standings alone, the whole point of this describe block.
    getStagesByFilters.mockResolvedValue({
      data: {
        items: [
          {
            id: guid('stage-group'),
            name: 'Zona A - Fase de Grupos',
            slug: 'zona-a-fase-de-grupos',
            stageType: StageType.Group,
            isActive: true,
            isElimination: false,
            startDate: '2026-01-01T00:00:00Z',
            endDate: '2026-02-01T00:00:00Z',
            divisionId: guid('division-1'),
            order: 0,
            bestOf: 1,
            roundRobinLegs: 1,
          } satisfies IStageResponse,
        ],
      },
    });
  });

  it('never shows the Playoff tab once stages are confirmed to have no elimination stage', async () => {
    render(
      <MemoryRouter>
        <PublicDivisionPanel division={division({ name: 'Zona A' })} teams={[]} />
      </MemoryRouter>
    );

    await waitFor(() => expect(getStagesByFilters).toHaveBeenCalled());
    expect(screen.queryByRole('tab', { name: 'Playoff' })).not.toBeInTheDocument();
  });

  it('crowns 1st place live, even mid-season — a no-playoff division reads its title straight off this table the whole way through, not just once it is decided', async () => {
    render(
      <MemoryRouter>
        <PublicDivisionPanel
          division={division({
            name: 'Zona A',
            isCrossDivisionCup: false,
            isFinished: false,
            positions: [position('team-a', 'Equipo A'), position('team-b', 'Equipo B')],
          })}
          teams={[]}
        />
      </MemoryRouter>
    );

    await waitFor(() => expect(screen.getByTitle('Campeón')).toBeInTheDocument());
  });

  it('does not crown 1st place once stages confirm the division actually has an elimination stage', async () => {
    getStagesByFilters.mockResolvedValue({
      data: {
        items: [
          {
            id: guid('stage-group'),
            name: 'Zona A - Fase de Grupos',
            slug: 'zona-a-fase-de-grupos',
            stageType: StageType.Group,
            isActive: true,
            isElimination: false,
            startDate: '2026-01-01T00:00:00Z',
            endDate: '2026-02-01T00:00:00Z',
            divisionId: guid('division-1'),
            order: 0,
            bestOf: 1,
            roundRobinLegs: 1,
          } satisfies IStageResponse,
          {
            id: guid('stage-final'),
            name: 'Zona A - Final',
            slug: 'zona-a-final',
            stageType: StageType.Final,
            isActive: true,
            isElimination: true,
            startDate: '2026-02-01T00:00:00Z',
            endDate: '2026-02-08T00:00:00Z',
            divisionId: guid('division-1'),
            order: 1,
            bestOf: 1,
            roundRobinLegs: 1,
          } satisfies IStageResponse,
        ],
      },
    });

    render(
      <MemoryRouter>
        <PublicDivisionPanel
          division={division({
            name: 'Zona A',
            isCrossDivisionCup: false,
            isFinished: false,
            positions: [position('team-a', 'Equipo A'), position('team-b', 'Equipo B')],
          })}
          teams={[]}
        />
      </MemoryRouter>
    );

    await waitFor(() => expect(getStagesByFilters).toHaveBeenCalled());
    expect(screen.queryByTitle('Campeón')).not.toBeInTheDocument();
  });
});
