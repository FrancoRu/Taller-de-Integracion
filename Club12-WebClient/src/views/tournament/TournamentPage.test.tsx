import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';
import TournamentPage from '@/views/tournament/TournamentPage';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { useAuth } from '@/modules/auth/hook/auth.hook';
import { TournamentStatus } from '@/modules/core/enum/tournament/tournamentStatus';
import { TournamentCategory } from '@/modules/core/enum/tournament/tournamentCategory';
import { UserRolesType } from '@/modules/core/enum/user/userRolesType';
import type { ITournamentContextProps, ITournamentResponse } from '@/modules/tournament/type/tournament.d';
import type { GUID } from '@/modules/core/types/types';

vi.mock('@/modules/tournament/hook/tournament.hook');
vi.mock('@/modules/auth/hook/auth.hook');
// The enrolled-teams tab pulls in team/tournament data hooks it does not need
// for this gate test; stub the whole subtree so only the tab wiring is exercised.
vi.mock('@/views/tournament/TournamentEnrolledTeams', () => ({
  default: () => <div>enrolled-teams-panel</div>,
}));
// The assignment tab pulls in division/stage/team/tournament data hooks it does
// not need for this gate test; stub the whole subtree so only the tab wiring is
// exercised.
vi.mock('@/views/tournament/TournamentDivisionAssignment', () => ({
  default: () => <div>division-assignment-panel</div>,
}));

const mockedUseTournament = vi.mocked(useTournament);
const mockedUseAuth = vi.mocked(useAuth);

const TOURNAMENT_ID = 'tournament-1' as unknown as GUID;

const buildTournament = (
  status: TournamentStatus
): ITournamentResponse => ({
  id: TOURNAMENT_ID,
  description: 'Torneo de prueba',
  name: 'Apertura',
  slug: 'apertura',
  divisions: [],
  teamRegistrationDeadline: new Date('2026-01-01'),
  startDate: new Date('2026-02-01'),
  status,
  category: TournamentCategory.Masculine,
  seasonId: null,
  seasonName: null,
});

const setup = (status: TournamentStatus) => {
  mockedUseTournament.mockReturnValue({
    tournament: buildTournament(status),
    tournaments: null,
    addTournament: vi.fn(),
    createFullTournament: vi.fn(),
    addFullDivision: vi.fn(),
    getAllTournamentsByFilter: vi.fn(),
    getTournamentById: vi.fn().mockResolvedValue(buildTournament(status)),
    putTournamentById: vi.fn(),
    deleteTournamentById: vi.fn(),
    registerTeamsByTournamentId: vi.fn(),
    enrollTeam: vi.fn(),
    unenrollTeam: vi.fn(),
    getCompletability: vi.fn(),
  } as ITournamentContextProps);

  mockedUseAuth.mockReturnValue({
    role: UserRolesType.Admin,
  } as ReturnType<typeof useAuth>);
};

const renderPage = () =>
  render(
    <MemoryRouter initialEntries={[`/panel/torneos/${TOURNAMENT_ID}`]}>
      <Routes>
        <Route
          path="/panel/torneos/:tournamentId"
          element={<TournamentPage />}
        />
        <Route path="/panel/torneos" element={<div>listado-torneos</div>} />
        <Route path="/panel/temporadas" element={<div>listado-temporadas</div>} />
      </Routes>
    </MemoryRouter>
  );

afterEach(() => {
  vi.clearAllMocks();
});

describe('TournamentPage — enrolled-teams tab gate (HU-107)', () => {
  it('shows the "Equipos inscriptos" tab only while OpenForRegistration', async () => {
    setup(TournamentStatus.OpenForRegistration);
    renderPage();

    expect(
      await screen.findByRole('tab', { name: 'Equipos inscriptos' })
    ).toBeInTheDocument();
  });

  it('hides the "Equipos inscriptos" tab for other statuses', async () => {
    setup(TournamentStatus.Scheduled);
    renderPage();

    await screen.findByRole('tab', { name: 'Detalle' });
    expect(
      screen.queryByRole('tab', { name: 'Equipos inscriptos' })
    ).not.toBeInTheDocument();
  });
});

describe('TournamentPage — read-only detail (QA wave 1)', () => {
  it('does NOT expose inline status editing on the "Ver" detail view', async () => {
    // Registration-closed is a status that previously offered a "Cambiar
    // estado" control right on the detail page. The detail is now read-only:
    // status changes belong to the "Editar torneo" path only.
    setup(TournamentStatus.RegistrationClosed);
    renderPage();

    await screen.findByRole('tab', { name: 'Detalle' });
    expect(screen.queryByText('Cambiar estado')).not.toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /Aplicar/i })
    ).not.toBeInTheDocument();
    // The edit affordance stays.
    expect(
      screen.getByRole('button', { name: 'Editar torneo' })
    ).toBeInTheDocument();
  });

  it('"Volver" navigates back to the seasons list (a tournament without a season)', async () => {
    // Tournaments live inside a season now; with no season, "Volver" falls back
    // to the seasons list rather than a standalone tournaments list.
    setup(TournamentStatus.Scheduled);
    renderPage();

    const volver = await screen.findByRole('button', { name: 'Volver' });
    await userEvent.click(volver);

    expect(screen.getByText('listado-temporadas')).toBeInTheDocument();
  });
});

describe('TournamentPage — assignment tab gate (HU-108/HU-109)', () => {
  it('shows the "Asignación" tab while registration is closed', async () => {
    setup(TournamentStatus.RegistrationClosed);
    renderPage();

    expect(
      await screen.findByRole('tab', { name: 'Asignación' })
    ).toBeInTheDocument();
  });

  it('shows the "Asignación" tab as a draft while registration is open', async () => {
    setup(TournamentStatus.OpenForRegistration);
    renderPage();

    expect(
      await screen.findByRole('tab', { name: 'Asignación' })
    ).toBeInTheDocument();
  });

  it('hides the "Asignación" tab once the tournament has started', async () => {
    setup(TournamentStatus.Ongoing);
    renderPage();

    await screen.findByRole('tab', { name: 'Detalle' });
    expect(
      screen.queryByRole('tab', { name: 'Asignación' })
    ).not.toBeInTheDocument();
  });
});
