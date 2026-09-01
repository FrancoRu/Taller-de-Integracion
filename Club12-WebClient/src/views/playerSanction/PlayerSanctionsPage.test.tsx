import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import PlayerSanctionsPage from '@/views/playerSanction/PlayerSanctionsPage';
import { usePlayerSanction } from '@/modules/playerSanction/hook/playerSanction.hook';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { useDivision } from '@/modules/division/hook/division.hook';
import { useStage } from '@/modules/stage/hook/stage.hook';
import { useMatch } from '@/modules/match/hook/match.hook';
import { usePlayer } from '@/modules/player/hook/player.hook';
import type { GUID } from '@/modules/core/types/types';

vi.mock('@/modules/playerSanction/hook/playerSanction.hook');
vi.mock('@/modules/tournament/hook/tournament.hook');
vi.mock('@/modules/division/hook/division.hook');
vi.mock('@/modules/stage/hook/stage.hook');
vi.mock('@/modules/match/hook/match.hook');
vi.mock('@/modules/player/hook/player.hook');
vi.mock('sweetalert2', () => ({ default: { fire: vi.fn() } }));

const buildSanction = () => ({
  id: 'sanction-1' as unknown as GUID,
  slug: 'sancion-1',
  duration: 2,
  fechasRemaining: 2,
  issuedDate: new Date('2026-01-01'),
  description: 'Falta grave',
  isActive: true,
  subjectType: 'Player',
  appealStatus: 'None',
});

describe('PlayerSanctionsPage — list actions', () => {
  it('does not offer an Editar row action — editing lives inside the sanction detail page', async () => {
    vi.mocked(usePlayerSanction).mockReturnValue({
      playerSanctions: [buildSanction()],
      getPlayerSanctionByFilter: vi.fn().mockResolvedValue({ totalCount: 1 }),
    } as unknown as ReturnType<typeof usePlayerSanction>);
    vi.mocked(useTournament).mockReturnValue({
      tournaments: [],
      getAllTournamentsByFilter: vi.fn(),
    } as unknown as ReturnType<typeof useTournament>);
    vi.mocked(useDivision).mockReturnValue({
      divisions: [],
      getDivisionsByFilters: vi.fn(),
    } as unknown as ReturnType<typeof useDivision>);
    vi.mocked(useStage).mockReturnValue({
      stages: [],
      getStagesByFilters: vi.fn(),
    } as unknown as ReturnType<typeof useStage>);
    vi.mocked(useMatch).mockReturnValue({
      matches: [],
      getMatchByFilter: vi.fn(),
    } as unknown as ReturnType<typeof useMatch>);
    vi.mocked(usePlayer).mockReturnValue({
      players: [],
      getPlayersByFilter: vi.fn(),
    } as unknown as ReturnType<typeof usePlayer>);

    render(
      <MemoryRouter>
        <PlayerSanctionsPage />
      </MemoryRouter>
    );

    await screen.findByText('Falta grave');
    expect(screen.queryByTestId('EditIcon')).not.toBeInTheDocument();
    expect(screen.getByTestId('VisibilityIcon')).toBeInTheDocument();
  });
});
