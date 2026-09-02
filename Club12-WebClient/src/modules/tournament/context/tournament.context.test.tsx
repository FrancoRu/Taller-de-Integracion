import { act, renderHook } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { ReactNode } from 'react';
import Swal from 'sweetalert2';
import { ErrorProvider } from '@/modules/error/context/error.context';
import { TournamentProvider } from '@/modules/tournament/context/tournament.context';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { tournamentService } from '@/modules/tournament/service/tournament.service';
import type { ITournamentResponse } from '@/modules/tournament/type/tournament.d';
import type { GUID } from '@/modules/core/types/types';
import { TournamentCategory } from '@/modules/core/enum/tournament/tournamentCategory';

vi.mock('@/modules/tournament/service/tournament.service');
vi.mock('sweetalert2', () => ({
  default: {
    fire: vi.fn(),
    getContainer: vi.fn().mockReturnValue(null),
  },
}));

const mockedGetAllTournamentsByFilter = vi.mocked(
  tournamentService.getAllTournamentsByFilter
);
const mockedCreateFullTournament = vi.mocked(
  tournamentService.createFullTournament
);
const mockedAddFullDivision = vi.mocked(tournamentService.addFullDivision);
const mockedRegisterTeams = vi.mocked(
  tournamentService.registerTeamsByTournamentId
);
const mockedSwalFire = vi.mocked(Swal.fire);

const TOURNAMENT_ID = 'guid-a-aaaa-bbbb-cccc' as unknown as GUID;

const buildTournament = (
  overrides: Partial<ITournamentResponse> = {}
): ITournamentResponse => ({
  id: 'guid-a-aaaa-bbbb-cccc' as unknown as GUID,
  description: 'Torneo de prueba',
  name: 'Apertura',
  slug: 'apertura',
  divisions: [],
  teamRegistrationDeadline: new Date('2026-01-01'),
  startDate: new Date('2026-02-01'),
  status: 'Scheduled',
  category: TournamentCategory.Masculine,
  seasonId: null,
  seasonName: null,
  ...overrides,
});

const wrapper = ({ children }: { children: ReactNode }) => (
  <ErrorProvider>
    <TournamentProvider>{children}</TournamentProvider>
  </ErrorProvider>
);

beforeEach(() => {
  vi.clearAllMocks();
});

describe('TournamentProvider — getAllTournamentsByFilter dedup guard', () => {
  /**
   * Each mocked call resolves a brand-new array reference (same ids/data),
   * so this test can only pass if the dedup guard's id-comparison genuinely
   * skips `setState` — not because React bails out on an identical object
   * reference on its own. The guard must also read the current `tournaments`
   * state (not a stale, mount-time closure) so it skips `setState` when the
   * fetched ids are unchanged on the second call.
   */
  it('keeps the tournaments reference stable when a repeated filter fetch returns the same items', async () => {
    mockedGetAllTournamentsByFilter.mockResolvedValueOnce({
      data: { items: [buildTournament()], page: 1, pageSize: 100, totalCount: 1 },
    } as never);
    mockedGetAllTournamentsByFilter.mockResolvedValueOnce({
      data: { items: [buildTournament()], page: 1, pageSize: 100, totalCount: 1 },
    } as never);

    const { result } = renderHook(() => useTournament(), { wrapper });

    await act(async () => {
      await result.current.getAllTournamentsByFilter({});
    });

    const firstReference = result.current.tournaments;
    expect(firstReference).not.toBeNull();

    await act(async () => {
      await result.current.getAllTournamentsByFilter({});
    });

    expect(result.current.tournaments).toBe(firstReference);
  });
});

describe('TournamentProvider — no duplicate success toast', () => {
  /**
   * The wizard (TournamentWizardPage), divisionCreatePage and TeamRegisterPage
   * each show their own confirmation for these actions. The context used to
   * ALSO fire a toast — an empty one for createFullTournament / addFullDivision
   * — so the user saw two modals for one action.
   */
  it('does not fire its own toast after createFullTournament succeeds', async () => {
    mockedCreateFullTournament.mockResolvedValueOnce({
      status: 200,
      data: buildTournament(),
    } as never);

    const { result } = renderHook(() => useTournament(), { wrapper });
    await act(async () => {
      await result.current.createFullTournament({} as never);
    });

    expect(mockedSwalFire).not.toHaveBeenCalled();
  });

  it('does not fire its own toast after addFullDivision succeeds', async () => {
    mockedAddFullDivision.mockResolvedValueOnce({
      status: 200,
      data: { id: 'div-1' },
    } as never);

    const { result } = renderHook(() => useTournament(), { wrapper });
    await act(async () => {
      await result.current.addFullDivision(TOURNAMENT_ID, {} as never);
    });

    expect(mockedSwalFire).not.toHaveBeenCalled();
  });

  it('does not fire its own toast after registerTeamsByTournamentId succeeds', async () => {
    mockedRegisterTeams.mockResolvedValueOnce({ status: 200 } as never);

    const { result } = renderHook(() => useTournament(), { wrapper });
    await act(async () => {
      await result.current.registerTeamsByTournamentId(TOURNAMENT_ID, []);
    });

    expect(mockedSwalFire).not.toHaveBeenCalled();
  });
});
