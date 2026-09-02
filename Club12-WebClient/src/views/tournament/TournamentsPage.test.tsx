import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';
import TournamentsPage from '@/views/tournament/TournamentsPage';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { useAuth } from '@/modules/auth/hook/auth.hook';
import { TournamentStatus } from '@/modules/core/enum/tournament/tournamentStatus';
import { TournamentCategory } from '@/modules/core/enum/tournament/tournamentCategory';
import { UserRolesType } from '@/modules/core/enum/user/userRolesType';
import type { ITournamentResponse } from '@/modules/tournament/type/tournament.d';
import type { GUID } from '@/modules/core/types/types';

vi.mock('@/modules/tournament/hook/tournament.hook');
vi.mock('@/modules/auth/hook/auth.hook');
vi.mock('sweetalert2', () => ({ default: { fire: vi.fn() } }));

import Swal from 'sweetalert2';

const mockedUseTournament = vi.mocked(useTournament);
const mockedUseAuth = vi.mocked(useAuth);
const mockedSwalFire = vi.mocked(Swal.fire);

const buildTournament = (
  overrides: Partial<ITournamentResponse> = {}
): ITournamentResponse => ({
  id: 'tournament-1' as unknown as GUID,
  description: 'Torneo de prueba',
  name: 'Apertura',
  slug: 'apertura',
  divisions: [],
  teamRegistrationDeadline: new Date('2026-01-01'),
  startDate: new Date('2026-02-01'),
  status: TournamentStatus.Scheduled,
  category: TournamentCategory.Masculine,
  seasonId: null,
  seasonName: null,
  ...overrides,
});

const renderPage = () =>
  render(
    <MemoryRouter>
      <TournamentsPage />
    </MemoryRouter>
  );

afterEach(() => {
  vi.clearAllMocks();
});

describe('TournamentsPage — delete failure', () => {
  it('does not show a success dialog or refetch when deleteTournamentById fails', async () => {
    mockedUseAuth.mockReturnValue({
      role: UserRolesType.Admin,
    } as ReturnType<typeof useAuth>);

    const getAllTournamentsByFilter = vi.fn().mockResolvedValue({
      items: [buildTournament()],
      totalCount: 1,
    });
    const deleteTournamentById = vi.fn().mockResolvedValue(false);
    mockedUseTournament.mockReturnValue({
      tournaments: [buildTournament()],
      getAllTournamentsByFilter,
      deleteTournamentById,
    } as unknown as ReturnType<typeof useTournament>);
    mockedSwalFire.mockResolvedValue({
      isConfirmed: true,
      isDenied: false,
      isDismissed: false,
    } as Awaited<ReturnType<typeof Swal.fire>>);

    renderPage();

    await screen.findByText('Apertura');
    getAllTournamentsByFilter.mockClear();

    const deleteIcon = await screen.findByTestId('DeleteIcon');
    (deleteIcon.closest('button') as HTMLButtonElement).click();

    await waitFor(() => expect(deleteTournamentById).toHaveBeenCalledTimes(1));

    expect(mockedSwalFire).not.toHaveBeenCalledWith(
      expect.objectContaining({ title: '¡Eliminado!' })
    );
    expect(getAllTournamentsByFilter).not.toHaveBeenCalled();
  });
});
