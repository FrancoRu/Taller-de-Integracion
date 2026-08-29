import { render, waitFor } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import PlayerPage from '@/views/player/PlayerPage';
import { PlayerProvider } from '@/modules/player/context/player.context';
import { sendGet } from '@/modules/core/utils/axiosUtils';
import { usePlayerStatistic } from '@/modules/playerStatistic/hook/playerStatistic.hook';
import { usePlayerSanction } from '@/modules/playerSanction/hook/playerSanction.hook';
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
vi.mock('@/modules/auth/hook/auth.hook');

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
const mockedUsePlayerStatistic = vi.mocked(usePlayerStatistic);
const mockedUsePlayerSanction = vi.mocked(usePlayerSanction);
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
      documentNumber: '30000001',
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
