import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { GUID } from '@/modules/core/types/types';
import {
  GroupStandings,
  IDivisionResponse,
  Position,
} from '@/modules/division/type/division.d';
import PublicDivisionPanel from '@/views/home/tournaments/PublicDivisionPanel';

// The POSICIONES tab renders synchronously from the division prop, but child
// components (PrintableResultsSheet) and the panel's other tabs lazily hit
// these services. Stub them so a bare render never makes a real request.
vi.mock('@/modules/scorer/service/scorer.service', () => ({
  scorerService: {
    getScorersByPlayerFiltered: vi.fn().mockResolvedValue({ data: { items: [] } }),
  },
}));
vi.mock('@/modules/stage/service/stage.service', () => ({
  stageService: { getStagesByFilters: vi.fn().mockResolvedValue({ data: { items: [] } }) },
}));
vi.mock('@/modules/match/service/match.service', () => ({
  matchService: { getMatchByFilter: vi.fn().mockResolvedValue({ data: { items: [] } }) },
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
