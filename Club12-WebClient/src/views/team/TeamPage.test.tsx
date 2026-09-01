import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { Mock } from 'vitest';
import TeamPage from '@/views/team/TeamPage';
import { useTeam } from '@/modules/team/hook/team.hook';
import { usePlayerStatistic } from '@/modules/playerStatistic/hook/playerStatistic.hook';
import { usePlayerSanction } from '@/modules/playerSanction/hook/playerSanction.hook';
import type { ITeamContextProps, ITeamResponse } from '@/modules/team/type/team.d';
import type { GUID } from '@/modules/core/types/types';

vi.mock('@/modules/team/hook/team.hook');
vi.mock('@/modules/playerStatistic/hook/playerStatistic.hook');
vi.mock('@/modules/playerSanction/hook/playerSanction.hook');
vi.mock('sweetalert2', () => ({ default: { fire: vi.fn() } }));

// These dialogs/pages pull in unrelated feature contexts (tournament, club,
// player list) that are irrelevant to the edit-trigger behavior under test.
vi.mock('@/views/playerStatistic/playerStatisticCreatePage', () => ({
  default: () => null,
}));
vi.mock('@/views/playerSanction/playerSanctionCreatePage', () => ({
  default: () => null,
}));
vi.mock('@/views/team/RosterImportDialog', () => ({ default: () => null }));
vi.mock('@/views/player/PlayersPage', () => ({ default: () => null }));

const mockedUseTeam = vi.mocked(useTeam);
const mockedUsePlayerStatistic = vi.mocked(usePlayerStatistic);
const mockedUsePlayerSanction = vi.mocked(usePlayerSanction);

const TEAM_ID = 'team-1-aaaa-bbbb-cccc' as unknown as GUID;

const buildTeam = (overrides: Partial<ITeamResponse> = {}): ITeamResponse => ({
  id: TEAM_ID,
  name: 'River',
  slug: 'river',
  threeLetterCode: 'RIV',
  shirtColor: '#1E5FCC',
  logoUrl: '',
  players: [],
  tournamentId: null,
  ...overrides,
});

let getTeamById: Mock<ITeamContextProps['getTeamById']>;
let putTeamById: Mock<ITeamContextProps['putTeamById']>;
let putTeamLogoById: Mock<ITeamContextProps['putTeamLogoById']>;

const setupHook = (team: ITeamResponse) => {
  getTeamById = vi.fn<ITeamContextProps['getTeamById']>();
  getTeamById.mockResolvedValue(team);
  putTeamById = vi.fn<ITeamContextProps['putTeamById']>();
  putTeamById.mockResolvedValue(true);
  putTeamLogoById = vi.fn<ITeamContextProps['putTeamLogoById']>();
  putTeamLogoById.mockResolvedValue(undefined);

  mockedUseTeam.mockReturnValue({
    team,
    teams: null,
    addTeam: vi.fn(),
    putTeamById,
    putTeamLogoById,
    getTeamsByFiltered: vi.fn(),
    getTeamById,
    deleteTeamById: vi.fn(),
  } as unknown as ITeamContextProps);

  mockedUsePlayerStatistic.mockReturnValue({
    playerStatistics: [],
    getPlayerStatisticsByFilter: vi.fn(),
  } as unknown as ReturnType<typeof usePlayerStatistic>);

  mockedUsePlayerSanction.mockReturnValue({
    playerSanctions: [],
    getPlayerSanctionByFilter: vi.fn(),
  } as unknown as ReturnType<typeof usePlayerSanction>);
};

const renderTeamPage = () =>
  render(
    <MemoryRouter initialEntries={[`/panel/equipos/${TEAM_ID}`]}>
      <Routes>
        <Route path="/panel/equipos/:teamId" element={<TeamPage />} />
      </Routes>
    </MemoryRouter>
  );

afterEach(() => {
  vi.clearAllMocks();
});

describe('TeamPage — edit trigger', () => {
  beforeEach(() => {
    setupHook(buildTeam());
  });

  it('does not render an edit dialog until "Editar equipo" is clicked', async () => {
    renderTeamPage();

    await screen.findByRole('button', { name: 'Editar equipo' });
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('opens the edit dialog prefilled with the team\'s current values', async () => {
    const user = userEvent.setup();
    renderTeamPage();

    await user.click(await screen.findByRole('button', { name: 'Editar equipo' }));

    const dialog = screen.getByRole('dialog');
    expect(within(dialog).getByRole('textbox', { name: /^Nombre/ })).toHaveValue(
      'River'
    );
    expect(within(dialog).getByRole('textbox', { name: /^Código/ })).toHaveValue(
      'RIV'
    );
  });

  it('saves via putTeamById, closes the dialog and refreshes the team', async () => {
    const user = userEvent.setup();
    renderTeamPage();

    await user.click(await screen.findByRole('button', { name: 'Editar equipo' }));

    const dialog = screen.getByRole('dialog');
    const nameInput = within(dialog).getByRole('textbox', { name: /^Nombre/ });
    await user.clear(nameInput);
    await user.type(nameInput, 'Racing');

    getTeamById.mockClear();
    await user.click(within(dialog).getByRole('button', { name: /guardar/i }));

    await waitFor(() => expect(putTeamById).toHaveBeenCalledTimes(1));
    const [id, payload] = putTeamById.mock.calls[0];
    expect(id).toBe(TEAM_ID);
    expect(payload).toEqual(expect.objectContaining({ name: 'Racing' }));

    await waitFor(() =>
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    );
    await waitFor(() => expect(getTeamById).toHaveBeenCalledTimes(1));
    expect(putTeamLogoById).not.toHaveBeenCalled();
  });
});
