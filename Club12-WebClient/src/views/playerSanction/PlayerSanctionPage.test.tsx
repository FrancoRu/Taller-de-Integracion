import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import PlayerSanctionPage from '@/views/playerSanction/PlayerSanctionPage';
import { usePlayerSanction } from '@/modules/playerSanction/hook/playerSanction.hook';
import { usePlayer } from '@/modules/player/hook/player.hook';
import { useMatch } from '@/modules/match/hook/match.hook';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import type { GUID } from '@/modules/core/types/types';

vi.mock('@/modules/playerSanction/hook/playerSanction.hook');
vi.mock('@/modules/player/hook/player.hook');
vi.mock('@/modules/match/hook/match.hook');
vi.mock('sweetalert2', () => ({ default: { fire: vi.fn() } }));

const mockNavigate = vi.fn();
vi.mock('react-router-dom', async importOriginal => {
  const actual = await importOriginal<typeof import('react-router-dom')>();
  return { ...actual, useNavigate: () => mockNavigate };
});

const SANCTION_ID = 'sanction-1' as unknown as GUID;

const buildSanction = () => ({
  id: SANCTION_ID,
  slug: 'sancion-1',
  duration: 2,
  fechasRemaining: 2,
  issuedDate: new Date('2026-01-01'),
  description: 'Falta grave',
  isActive: true,
  subjectType: 'Player',
  appealStatus: 'None',
});

const renderAt = () =>
  render(
    <MemoryRouter initialEntries={[`/panel/sanciones/${SANCTION_ID}`]}>
      <Routes>
        <Route path="/panel/sanciones/:playerSanctionId" element={<PlayerSanctionPage />} />
      </Routes>
    </MemoryRouter>
  );

describe('PlayerSanctionPage — Editar trigger', () => {
  it('navigates to the edit route when "Editar sanción" is clicked', async () => {
    vi.mocked(usePlayerSanction).mockReturnValue({
      playerSanction: buildSanction(),
      getPlayerSanctionById: vi.fn().mockResolvedValue(buildSanction()),
      appealPlayerSanction: vi.fn(),
      resolvePlayerSanctionAppeal: vi.fn(),
    } as unknown as ReturnType<typeof usePlayerSanction>);
    vi.mocked(usePlayer).mockReturnValue({
      player: null,
      getPlayerById: vi.fn(),
    } as unknown as ReturnType<typeof usePlayer>);
    vi.mocked(useMatch).mockReturnValue({
      match: null,
      getMatchById: vi.fn(),
    } as unknown as ReturnType<typeof useMatch>);

    const user = userEvent.setup();
    renderAt();

    await user.click(await screen.findByRole('button', { name: 'Editar sanción' }));

    expect(mockNavigate).toHaveBeenCalledWith(
      APP_ROUTES.panelSanctionEdit.build('sancion-1')
    );
  });
});
