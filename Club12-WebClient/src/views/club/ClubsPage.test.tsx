import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { Mock } from 'vitest';
import ClubsPage from '@/views/club/ClubsPage';
import { useClub } from '@/modules/club/hook/club.hook';
import { useTeam } from '@/modules/team/hook/team.hook';
import type { IClubContextProps, IClubSummaryResponse } from '@/modules/club/type/club.d';
import type { ITeamContextProps, ITeamResponse } from '@/modules/team/type/team.d';
import type { GUID } from '@/modules/core/types/types';

const mockedNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return { ...actual, useNavigate: () => mockedNavigate };
});

vi.mock('@/modules/club/hook/club.hook');
vi.mock('@/modules/team/hook/team.hook');

const mockedUseClub = vi.mocked(useClub);
const mockedUseTeam = vi.mocked(useTeam);

let guidCounter = 0;
const nextGuid = (): GUID => `guid-${guidCounter++}-aaaa-bbbb-cccc` as unknown as GUID;

const buildClub = (overrides: Partial<IClubSummaryResponse> = {}): IClubSummaryResponse => ({
  id: nextGuid(),
  name: 'River',
  slug: 'river',
  logoUrl: null,
  ...overrides,
});

let getAllClubs: Mock<IClubContextProps['getAllClubs']>;
let addTeam: Mock<ITeamContextProps['addTeam']>;

const setupHook = (clubs: IClubSummaryResponse[] = [buildClub()]) => {
  getAllClubs = vi.fn<IClubContextProps['getAllClubs']>();
  getAllClubs.mockResolvedValue(clubs);
  addTeam = vi.fn<ITeamContextProps['addTeam']>();
  addTeam.mockResolvedValue({} as ITeamResponse);

  mockedUseClub.mockReturnValue({
    club: null,
    getClubHistory: vi.fn(),
    copyRoster: vi.fn(),
    allClubs: clubs,
    getAllClubs,
    linkClubParent: vi.fn(),
    unlinkClubParent: vi.fn(),
    renameClub: vi.fn(),
  } satisfies IClubContextProps);

  mockedUseTeam.mockReturnValue({
    team: null,
    teams: null,
    addTeam,
    putTeamById: vi.fn(),
    putTeamLogoById: vi.fn(),
    getTeamsByFiltered: vi.fn(),
    getTeamById: vi.fn(),
    deleteTeamById: vi.fn(),
  } satisfies ITeamContextProps);
};

const renderPage = () =>
  render(
    <MemoryRouter>
      <ClubsPage />
    </MemoryRouter>
  );

beforeEach(() => {
  setupHook();
});

afterEach(() => {
  vi.clearAllMocks();
});

describe('ClubsPage', () => {
  it('fetches and lists every club — one row per stable club, not per per-season team', async () => {
    setupHook([buildClub({ name: 'River' }), buildClub({ name: 'Boca' })]);
    renderPage();

    await waitFor(() => expect(getAllClubs).toHaveBeenCalled());
    expect(await screen.findByText('River')).toBeInTheDocument();
    expect(screen.getByText('Boca')).toBeInTheDocument();
  });

  it('filters the list by name as the admin types', async () => {
    setupHook([buildClub({ name: 'River' }), buildClub({ name: 'Boca' })]);
    const user = userEvent.setup();
    renderPage();

    await screen.findByText('River');
    await user.type(screen.getByLabelText('Nombre'), 'riv');

    expect(screen.getByText('River')).toBeInTheDocument();
    expect(screen.queryByText('Boca')).not.toBeInTheDocument();
  });

  it('navigates to the club detail page when a row is clicked', async () => {
    setupHook([buildClub({ name: 'River', slug: 'river' })]);
    const user = userEvent.setup();
    renderPage();

    await user.click(await screen.findByText('River'));

    expect(mockedNavigate).toHaveBeenCalledWith('/panel/clubes/river');
  });
});
