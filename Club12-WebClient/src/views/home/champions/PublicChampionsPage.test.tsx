import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
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

const renderPage = () =>
  render(
    <MemoryRouter>
      <PublicChampionsPage />
    </MemoryRouter>
  );

const entry = (overrides: Partial<IChampionHistory> = {}): IChampionHistory => ({
  tournamentId: guid('tournament-1'),
  tournamentName: 'Apertura 2025',
  seasonName: 'Temporada 2025',
  seasonYear: 2025,
  category: TournamentCategory.Masculine,
  divisionName: 'Zona A',
  cupName: null,
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
          tournamentId: guid('tournament-2'),
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

    renderPage();

    await waitFor(() => {
      expect(screen.getByText(/Apertura 2025/)).toBeInTheDocument();
    });

    expect(screen.getByRole('heading', { name: 'Temporada 2025' })).toBeInTheDocument();
    expect(screen.getByText('Los Halcones')).toBeInTheDocument();
    expect(screen.getByText('Las Panteras')).toBeInTheDocument();
    expect(screen.getByText('Masculino')).toBeInTheDocument();
    expect(screen.getByText('Femenino')).toBeInTheDocument();

    // Each champion team links to its public team page.
    expect(
      screen.getByRole('link', { name: /Los Halcones/i })
    ).toHaveAttribute('href', '/equipos/team-1');
  });

  it('opens only the newest season and keeps older seasons collapsed', async () => {
    const user = userEvent.setup();
    getChampionsHistory.mockResolvedValue({
      data: [
        entry({
          tournamentId: guid('t-2026'),
          tournamentName: 'Apertura 2026',
          seasonName: 'Temporada 2026',
          seasonYear: 2026,
        }),
        entry({
          tournamentId: guid('t-2025'),
          tournamentName: 'Apertura 2025',
          seasonName: 'Temporada 2025',
          seasonYear: 2025,
        }),
      ],
    });

    renderPage();

    const newest = await screen.findByRole('button', { name: /Temporada 2026/ });
    const older = screen.getByRole('button', { name: /Temporada 2025/ });

    expect(newest).toHaveAttribute('aria-expanded', 'true');
    expect(older).toHaveAttribute('aria-expanded', 'false');
    expect(screen.getByText('Apertura 2026')).toBeVisible();

    await user.click(older);
    expect(older).toHaveAttribute('aria-expanded', 'true');
    expect(await screen.findByText('Apertura 2025')).toBeVisible();
  });

  it('shows the empty state when there are no champions yet', async () => {
    getChampionsHistory.mockResolvedValue({ data: [] });

    renderPage();

    await waitFor(() => {
      expect(
        screen.getByText(/Todavía no hay campeones/i)
      ).toBeInTheDocument();
    });
  });
});
