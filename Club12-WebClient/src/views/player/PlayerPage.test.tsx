import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import PlayerPage from '@/views/player/PlayerPage';
import { PlayerProvider } from '@/modules/player/context/player.context';
import { sendGet, sendPut } from '@/modules/core/utils/axiosUtils';
import { usePlayerStatistic } from '@/modules/playerStatistic/hook/playerStatistic.hook';
import { usePlayerSanction } from '@/modules/playerSanction/hook/playerSanction.hook';
import { useTeam } from '@/modules/team/hook/team.hook';
import { useAuth } from '@/modules/auth/hook/auth.hook';

vi.mock('@/modules/core/utils/axiosUtils', () => ({
  sendGet: vi.fn(),
  sendPost: vi.fn(),
  sendPut: vi.fn(),
  sendDelete: vi.fn(),
}));

// PlayerProvider funnels failures through the global error context; the page
// itself does not need it for this characterization.
vi.mock('@/modules/error/hooks/useUnknownErrorHandler', () => ({
  useUnknownErrorHandler: () => vi.fn(),
}));

vi.mock('@/modules/playerStatistic/hook/playerStatistic.hook');
vi.mock('@/modules/playerSanction/hook/playerSanction.hook');
vi.mock('@/modules/team/hook/team.hook');
vi.mock('@/modules/auth/hook/auth.hook');
vi.mock('sweetalert2', () => ({ default: { fire: vi.fn() } }));

const mockNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
  const actual =
    await vi.importActual<typeof import('react-router-dom')>('react-router-dom');
  return { ...actual, useNavigate: () => mockNavigate };
});

// Child dialogs/cards pull in unrelated feature contexts (match, tournament,
// division, stage) that are irrelevant to the fetch under test.
vi.mock('@/views/playerStatistic/playerStatisticCreatePage', () => ({
  default: () => null,
}));
vi.mock('@/views/playerSanction/playerSanctionCreatePage', () => ({
  default: () => null,
}));
vi.mock('@/views/playerStatistic/PlayerStatisticCard', () => ({
  default: () => null,
}));
vi.mock('@/views/playerStatistic/PlayerHistory', () => ({ default: () => null }));

const mockedSendGet = vi.mocked(sendGet);
const mockedSendPut = vi.mocked(sendPut);
const mockedUsePlayerStatistic = vi.mocked(usePlayerStatistic);
const mockedUsePlayerSanction = vi.mocked(usePlayerSanction);
const mockedUseTeam = vi.mocked(useTeam);
const mockedUseAuth = vi.mocked(useAuth);

const renderAt = (param: string) =>
  render(
    <QueryClientProvider client={new QueryClient()}>
      <PlayerProvider>
        <MemoryRouter initialEntries={[`/panel/jugadores/${param}`]}>
          <Routes>
            <Route path="/panel/jugadores/:playerId" element={<PlayerPage />} />
          </Routes>
        </MemoryRouter>
      </PlayerProvider>
    </QueryClientProvider>
  );

beforeEach(() => {
  mockedSendGet.mockResolvedValue({
    data: {
      id: '11111111-1111-1111-1111-111111111111',
      slug: 'lopez-carlos',
      fullName: 'LÓPEZ Carlos',
      firstName: 'Carlos',
      lastName: 'López',
      documentNumber: '30000001',
      teamId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    },
  } as Awaited<ReturnType<typeof sendGet>>);

  mockedUsePlayerStatistic.mockReturnValue({
    playerStatistics: [],
    getPlayerStatisticsByFilter: vi.fn(),
    playerCard: null,
    getPlayerCard: vi.fn(),
    playerHistory: null,
    getPlayerHistory: vi.fn(),
  } as unknown as ReturnType<typeof usePlayerStatistic>);

  mockedUsePlayerSanction.mockReturnValue({
    playerSanctions: [],
    getPlayerSanctionByFilter: vi.fn(),
  } as unknown as ReturnType<typeof usePlayerSanction>);

  mockedUseTeam.mockReturnValue({
    teams: [{ id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', name: 'River' }],
    getTeamsByFiltered: vi.fn(),
  } as unknown as ReturnType<typeof useTeam>);

  mockedUseAuth.mockReturnValue({
    role: 'ADMIN',
  } as unknown as ReturnType<typeof useAuth>);
});

afterEach(() => {
  vi.clearAllMocks();
});

describe('PlayerPage — admin detail fetch', () => {
  it('issues GET players/admin/{param} with the raw route param (slug form)', async () => {
    renderAt('lopez-carlos');

    await waitFor(() =>
      expect(mockedSendGet).toHaveBeenCalledWith('players/admin/lopez-carlos')
    );
  });

  it('issues GET players/admin/{param} with the raw route param (guid form)', async () => {
    renderAt('22222222-2222-2222-2222-222222222222');

    await waitFor(() =>
      expect(mockedSendGet).toHaveBeenCalledWith(
        'players/admin/22222222-2222-2222-2222-222222222222'
      )
    );
  });
});

describe('PlayerPage — edit trigger', () => {
  it('opens the edit dialog prefilled with the player\'s current values', async () => {
    const user = userEvent.setup();
    renderAt('lopez-carlos');

    await user.click(await screen.findByRole('button', { name: 'Editar jugador' }));

    const dialog = screen.getByRole('dialog');
    expect(within(dialog).getByRole('textbox', { name: /^Documento/ })).toHaveValue(
      '30000001'
    );
  });

  it('saves via putPlayerById, closes the dialog and refreshes the player', async () => {
    mockedSendPut.mockResolvedValue({
      data: { id: '11111111-1111-1111-1111-111111111111' },
    } as Awaited<ReturnType<typeof sendPut>>);

    const user = userEvent.setup();
    renderAt('lopez-carlos');

    await user.click(await screen.findByRole('button', { name: 'Editar jugador' }));

    const dialog = screen.getByRole('dialog');
    await user.type(
      within(dialog).getByRole('textbox', { name: /^Nombre/ }),
      'Carlos'
    );
    await user.type(
      within(dialog).getByRole('textbox', { name: /^Apellido/ }),
      'López'
    );
    await user.type(
      within(dialog).getByLabelText(/Fecha de nacimiento/),
      '2000-01-01'
    );
    await user.type(
      within(dialog).getByRole('textbox', { name: /^Teléfono/ }),
      '3510000000'
    );
    await user.type(
      within(dialog).getByRole('textbox', { name: /^Obra social/ }),
      'OSDE'
    );

    mockedSendGet.mockClear();
    await user.click(within(dialog).getByRole('button', { name: /guardar/i }));

    await waitFor(() => expect(mockedSendPut).toHaveBeenCalledTimes(1));
    const [url] = mockedSendPut.mock.calls[0];
    expect(url).toBe('players/11111111-1111-1111-1111-111111111111');

    await waitFor(() =>
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    );
    await waitFor(() =>
      expect(mockedSendGet).toHaveBeenCalledWith(
        'players/admin/11111111-1111-1111-1111-111111111111'
      )
    );
  });
});

describe('PlayerPage — "Volver" navigation', () => {
  it('goes back via real browser history — not a hardcoded destination that drops whatever tab/list the admin actually came from', async () => {
    const user = userEvent.setup();
    renderAt('lopez-carlos');

    await user.click(await screen.findByRole('button', { name: /Volver/ }));

    expect(mockNavigate).toHaveBeenCalledWith(-1);
  });
});
