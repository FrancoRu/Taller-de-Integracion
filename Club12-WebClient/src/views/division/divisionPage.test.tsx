import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';
import DivisionPage from '@/views/division/divisionPage';
import { useDivision } from '@/modules/division/hook/division.hook';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { useStage } from '@/modules/stage/hook/stage.hook';
import { useAuth } from '@/modules/auth/hook/auth.hook';
import { IDivisionResponse } from '@/modules/division/type/division';
import { IStageResponse, StageType } from '@/modules/stage/type/stage';
import { IMatchResponse } from '@/modules/match/type/match.d';
import { MatchType } from '@/modules/core/enum/match/matchType';
import { UserRolesType } from '@/modules/core/enum/user/userRolesType';
import type { GUID } from '@/modules/core/types/types';

vi.mock('@/modules/division/hook/division.hook');
vi.mock('@/modules/tournament/hook/tournament.hook');
vi.mock('@/modules/stage/hook/stage.hook');
vi.mock('@/modules/auth/hook/auth.hook');

const getStagesByFilters = vi.fn().mockResolvedValue({ data: { items: [] } });
const getMatchByFilter = vi.fn().mockResolvedValue({ data: { items: [] } });

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

const getScorersByPlayerFiltered = vi.fn().mockResolvedValue({ data: { items: [] } });

vi.mock('@/modules/scorer/service/scorer.service', () => ({
  scorerService: {
    getScorersByPlayerFiltered: (...args: unknown[]) => getScorersByPlayerFiltered(...args),
  },
}));

const mockedUseDivision = vi.mocked(useDivision);
const mockedUseTournament = vi.mocked(useTournament);
const mockedUseStage = vi.mocked(useStage);
const mockedUseAuth = vi.mocked(useAuth);

const guid = (value: string) => value as GUID;
const DIVISION_ID = guid('division-1');

const buildDivision = (): IDivisionResponse =>
  ({
    id: DIVISION_ID,
    name: 'Zona A',
    slug: 'zona-a',
    isFinished: false,
    tournamentId: guid('tournament-1'),
    isCrossDivisionCup: false,
    positions: [],
  }) as IDivisionResponse;

const koStage: IStageResponse = {
  id: guid('stage-final'),
  name: 'Zona A - Final',
  slug: 'zona-a-final',
  stageType: StageType.Final,
  isActive: true,
  isElimination: true,
  startDate: '2026-02-01T00:00:00Z',
  endDate: '2026-02-08T00:00:00Z',
  divisionId: DIVISION_ID,
  order: 1,
  bestOf: 1,
  roundRobinLegs: 1,
};

const buildMatch = (id: string, stageId: GUID, opponentName: string): IMatchResponse => ({
  id: guid(id),
  matchDate: '2026-02-01T20:00:00Z',
  matchType: MatchType.Playoff,
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
  isFinished: true,
  winningTeamId: guid('team-home'),
  winningTeamName: 'Equipo Local',
  venue: null,
  stageId,
});

const setup = () => {
  mockedUseDivision.mockReturnValue({
    division: buildDivision(),
    divisions: null,
    getDivisionsById: vi.fn().mockResolvedValue(buildDivision()),
  } as unknown as ReturnType<typeof useDivision>);

  mockedUseTournament.mockReturnValue({
    tournament: null,
    getTournamentById: vi.fn(),
  } as unknown as ReturnType<typeof useTournament>);

  mockedUseStage.mockReturnValue({
    seedKnockoutStage: vi.fn(),
  } as unknown as ReturnType<typeof useStage>);

  mockedUseAuth.mockReturnValue({
    role: UserRolesType.Admin,
  } as unknown as ReturnType<typeof useAuth>);

  getStagesByFilters.mockResolvedValue({ data: { items: [koStage] } });
  getMatchByFilter.mockResolvedValue({
    data: { items: [buildMatch('match-final', koStage.id, 'Rival de la final')] },
  });
};

const renderPage = (tab = 'playoff') =>
  render(
    <MemoryRouter initialEntries={[`/panel/divisiones/${DIVISION_ID}?tab=${tab}`]}>
      <Routes>
        <Route path="/panel/divisiones/:divisionId" element={<DivisionPage />} />
      </Routes>
    </MemoryRouter>
  );

afterEach(() => {
  vi.clearAllMocks();
});

describe('DivisionPage — Playoff tab shows a real match list alongside the bracket', () => {
  it('renders the elimination-stage match as a real fixture row', async () => {
    setup();
    renderPage();

    await waitFor(() =>
      expect(screen.getByRole('heading', { name: 'Partidos' })).toBeInTheDocument()
    );
    expect(screen.getAllByText('Rival de la final').length).toBeGreaterThan(0);
  });
});

describe('DivisionPage — Goleadores tab', () => {
  it('renders the division scorers ranking fetched by divisionId', async () => {
    setup();
    getScorersByPlayerFiltered.mockResolvedValue({
      data: {
        items: [
          {
            playerId: guid('player-1'),
            fullName: 'PEREZ Juan',
            points: 24,
            jerseyNumber: 7,
            teamId: guid('team-home'),
            teamName: 'Equipo Local',
          },
        ],
      },
    });

    renderPage('goleadores');

    await waitFor(() => expect(screen.getByText('PEREZ Juan')).toBeInTheDocument());
    expect(getScorersByPlayerFiltered).toHaveBeenCalledWith(
      expect.objectContaining({ divisionId: DIVISION_ID })
    );
  });
});
