import { render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { GUID } from '@/modules/core/types/types';
import { IChampionHistory } from '@/modules/champion/type/champion.d';
import { TournamentCategory } from '@/modules/core/enum/tournament/tournamentCategory';
import PublicChampionsPage from '@/views/home/champions/PublicChampionsPage';

const getChampionsHistory = vi.fn();

vi.mock('@/modules/champion/service/champion.service', () => ({
  championService: {
    getChampionsHistory: (...args: unknown[]) => getChampionsHistory(...args),
  },
}));

const guid = (value: string) => value as GUID;

const entry = (overrides: Partial<IChampionHistory> = {}): IChampionHistory => ({
  tournamentId: guid('tournament-1'),
  tournamentName: 'Apertura 2025',
  seasonName: 'Temporada 2025',
  category: TournamentCategory.Masculine,
  divisionName: 'Zona A',
  championTeam: {
    teamId: guid('team-1'),
    teamName: 'Los Halcones',
    logoUrl: null,
  },
  ...overrides,
});

describe('PublicChampionsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders the champion history rows grouped by season', async () => {
    getChampionsHistory.mockResolvedValue({
      data: [
        entry(),
        entry({
          tournamentName: 'Clausura 2025',
          divisionName: 'Zona Única',
          category: TournamentCategory.Feminine,
          championTeam: {
            teamId: guid('team-2'),
            teamName: 'Las Panteras',
            logoUrl: null,
          },
        }),
      ],
    });

    render(<PublicChampionsPage />);

    await waitFor(() => {
      expect(screen.getByText('Apertura 2025')).toBeInTheDocument();
    });

    expect(screen.getByRole('heading', { name: 'Temporada 2025' })).toBeInTheDocument();
    expect(screen.getByText('Los Halcones')).toBeInTheDocument();
    expect(screen.getByText('Las Panteras')).toBeInTheDocument();
    expect(screen.getByText('Masculino')).toBeInTheDocument();
    expect(screen.getByText('Femenino')).toBeInTheDocument();
  });

  it('shows the empty state when there are no champions yet', async () => {
    getChampionsHistory.mockResolvedValue({ data: [] });

    render(<PublicChampionsPage />);

    await waitFor(() => {
      expect(
        screen.getByText(/Todavía no hay campeones/i)
      ).toBeInTheDocument();
    });
  });
});
