import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import PublicTournamentPage from '@/views/home/tournaments/PublicTournamentPage';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { useTeam } from '@/modules/team/hook/team.hook';
import { useDivision } from '@/modules/division/hook/division.hook';
import { TournamentStatus } from '@/modules/core/enum/tournament/tournamentStatus';
import { TournamentCategory } from '@/modules/core/enum/tournament/tournamentCategory';
import type { GUID } from '@/modules/core/types/types';
import type {
  ITournamentContextProps,
  ITournamentResponse,
} from '@/modules/tournament/type/tournament.d';
import type { ITeamContextProps } from '@/modules/team/type/team.d';
import type { IDivisionContextProps } from '@/modules/division/type/division.d';

vi.mock('@/modules/tournament/hook/tournament.hook');
vi.mock('@/modules/team/hook/team.hook');
vi.mock('@/modules/division/hook/division.hook');

const mockedUseTournament = vi.mocked(useTournament);
const mockedUseTeam = vi.mocked(useTeam);
const mockedUseDivision = vi.mocked(useDivision);

const TOURNAMENT_ID = 'tournament-1' as unknown as GUID;

const buildTournament = (): ITournamentResponse => ({
  id: TOURNAMENT_ID,
  description: 'Torneo de prueba',
  name: 'Apertura 2026',
  slug: 'apertura-2026',
  divisions: [],
  teamRegistrationDeadline: new Date('2026-01-01'),
  startDate: new Date('2026-02-01'),
  status: TournamentStatus.OpenForRegistration,
  category: TournamentCategory.Masculine,
  seasonId: null,
  seasonName: null,
});

const renderPage = (
  options: { initialEntries?: string[]; initialIndex?: number } = {}
) =>
  render(
    <MemoryRouter
      initialEntries={options.initialEntries ?? [`/torneos/${TOURNAMENT_ID}`]}
      initialIndex={options.initialIndex}
    >
      <Routes>
        <Route path="/torneos/:tournamentId" element={<PublicTournamentPage />} />
        <Route path="/temporadas" element={<div>listado-temporadas</div>} />
        <Route
          path="/temporadas/:seasonId"
          element={<div>detalle-temporada</div>}
        />
      </Routes>
    </MemoryRouter>
  );

const mockDivisionsAndTeams = () => {
  mockedUseDivision.mockReturnValue({
    division: null,
    divisions: null,
    addDivision: vi.fn(),
    generateFixtureByDivisionId: vi.fn(),
    putDivisionById: vi.fn(),
    getDivisionsById: vi.fn(),
    getDivisionsByFilters: vi
      .fn()
      .mockResolvedValue({ items: [], page: 1, pageSize: 20, totalCount: 0 }),
    deleteDivisionsById: vi.fn(),
  } as IDivisionContextProps);

  mockedUseTeam.mockReturnValue({
    team: null,
    teams: null,
    addTeam: vi.fn(),
    putTeamById: vi.fn(),
    putTeamLogoById: vi.fn(),
    getTeamsByFiltered: vi
      .fn()
      .mockResolvedValue({ items: [], page: 1, pageSize: 20, totalCount: 0 }),
    getTeamById: vi.fn(),
    deleteTeamById: vi.fn(),
  } as ITeamContextProps);
};

describe('PublicTournamentPage — loading gate', () => {
  it('keeps the skeleton up while the teams fetch is still in flight, even after tournament and divisions resolve', async () => {
    mockedUseTournament.mockReturnValue({
      tournament: buildTournament(),
      tournaments: null,
      addTournament: vi.fn(),
      createFullTournament: vi.fn(),
      addFullDivision: vi.fn(),
      getAllTournamentsByFilter: vi.fn(),
      getTournamentById: vi.fn().mockResolvedValue(buildTournament()),
      putTournamentById: vi.fn(),
      deleteTournamentById: vi.fn(),
      enrollTeam: vi.fn(),
      unenrollTeam: vi.fn(),
      getCompletability: vi.fn(),
    } as ITournamentContextProps);

    mockedUseDivision.mockReturnValue({
      division: null,
      divisions: null,
      addDivision: vi.fn(),
      generateFixtureByDivisionId: vi.fn(),
      putDivisionById: vi.fn(),
      getDivisionsById: vi.fn(),
      getDivisionsByFilters: vi
        .fn()
        .mockResolvedValue({ items: [], page: 1, pageSize: 20, totalCount: 0 }),
      deleteDivisionsById: vi.fn(),
    } as IDivisionContextProps);

    let resolveTeams: (value: unknown) => void = () => {};
    const pendingTeams = new Promise(resolve => {
      resolveTeams = resolve;
    });
    mockedUseTeam.mockReturnValue({
      team: null,
      teams: null,
      addTeam: vi.fn(),
      putTeamById: vi.fn(),
      putTeamLogoById: vi.fn(),
      getTeamsByFiltered: vi.fn().mockReturnValue(pendingTeams),
      getTeamById: vi.fn(),
      deleteTeamById: vi.fn(),
    } as ITeamContextProps);

    renderPage();

    // Tournament + divisions already resolved (both are synchronous/instant
    // above), but the teams fetch is still pending — the page must still be
    // on its skeleton, not the real tab content.
    await waitFor(() => expect(screen.queryByRole('tablist')).not.toBeInTheDocument());

    resolveTeams({ items: [], page: 1, pageSize: 20, totalCount: 0 });

    await waitFor(() => expect(screen.getByRole('tablist')).toBeInTheDocument());
  });
});

describe('PublicTournamentPage — "Volver" target', () => {
  it('goes back via real browser history, landing on the season page it actually came from', async () => {
    const tournamentWithSeason: ITournamentResponse = {
      ...buildTournament(),
      seasonId: 'season-1' as unknown as GUID,
      seasonSlug: 'xxvii-temporada',
      seasonName: 'XXVII Temporada',
    };

    mockedUseTournament.mockReturnValue({
      tournament: tournamentWithSeason,
      tournaments: null,
      addTournament: vi.fn(),
      createFullTournament: vi.fn(),
      addFullDivision: vi.fn(),
      getAllTournamentsByFilter: vi.fn(),
      getTournamentById: vi.fn().mockResolvedValue(tournamentWithSeason),
      putTournamentById: vi.fn(),
      deleteTournamentById: vi.fn(),
      enrollTeam: vi.fn(),
      unenrollTeam: vi.fn(),
      getCompletability: vi.fn(),
    } as ITournamentContextProps);
    mockDivisionsAndTeams();

    // A real prior history entry — "Volver" is real browser-history back,
    // not a reconstructed URL that would always land on the season's
    // default tab regardless of where "here" actually was.
    renderPage({
      initialEntries: ['/temporadas/xxvii-temporada', `/torneos/${TOURNAMENT_ID}`],
      initialIndex: 1,
    });

    const back = await screen.findByRole('button', {
      name: /Volver a XXVII Temporada/,
    });
    await userEvent.click(back);

    expect(screen.getByText('detalle-temporada')).toBeInTheDocument();
  });

  it('labels the button "Volver a temporadas" when the tournament has no season', async () => {
    mockedUseTournament.mockReturnValue({
      tournament: buildTournament(),
      tournaments: null,
      addTournament: vi.fn(),
      createFullTournament: vi.fn(),
      addFullDivision: vi.fn(),
      getAllTournamentsByFilter: vi.fn(),
      getTournamentById: vi.fn().mockResolvedValue(buildTournament()),
      putTournamentById: vi.fn(),
      deleteTournamentById: vi.fn(),
      enrollTeam: vi.fn(),
      unenrollTeam: vi.fn(),
      getCompletability: vi.fn(),
    } as ITournamentContextProps);
    mockDivisionsAndTeams();

    renderPage({
      initialEntries: ['/temporadas', `/torneos/${TOURNAMENT_ID}`],
      initialIndex: 1,
    });

    const back = await screen.findByRole('button', {
      name: /Volver a temporadas/,
    });
    await userEvent.click(back);

    expect(screen.getByText('listado-temporadas')).toBeInTheDocument();
  });
});
