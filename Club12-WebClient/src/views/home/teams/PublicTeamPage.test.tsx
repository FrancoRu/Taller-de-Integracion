import { render, screen, waitFor, within } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { GUID } from '@/modules/core/types/types';
import { TournamentCategory } from '@/modules/core/enum/tournament/tournamentCategory';
import {
  TeamMatch,
  TeamParticipation,
  TeamSummary,
} from '@/modules/team/type/teamProfile.d';
import PublicTeamPage from '@/views/home/teams/PublicTeamPage';

const guid = (value: string) => value as GUID;

const TEAM_ID = 'team-1';

// Hoisted so the mocked `useTeam` returns a STABLE team object and a stable
// `getTeamById` reference across renders — mirroring the real context's
// `useCallback`, which the page's effect deps rely on.
const { mockTeam, getTeamByIdMock } = vi.hoisted(() => ({
  mockTeam: {
    id: 'team-1' as GUID,
    name: 'Los Halcones',
    slug: 'team-1',
    threeLetterCode: 'HAL',
    shirtColor: '#FF5A1F',
    shirtSecondaryColor: '#000000',
    jerseyStyle: 'solid',
    logoUrl: null,
    players: [],
    tournamentId: null,
  },
  getTeamByIdMock: vi.fn().mockResolvedValue(undefined),
}));

vi.mock('@/modules/team/hook/team.hook', () => ({
  useTeam: () => ({ team: mockTeam, getTeamById: getTeamByIdMock }),
}));

const getTeamParticipations = vi.fn();
const getTeamSummary = vi.fn();
const getTeamMatches = vi.fn();

vi.mock('@/modules/team/service/team.service', () => ({
  teamService: {
    getTeamParticipations: (...args: unknown[]) => getTeamParticipations(...args),
    getTeamSummary: (...args: unknown[]) => getTeamSummary(...args),
    getTeamMatches: (...args: unknown[]) => getTeamMatches(...args),
  },
}));

const getScorersByPlayerFiltered = vi.fn();

vi.mock('@/modules/scorer/service/scorer.service', () => ({
  scorerService: {
    getScorersByPlayerFiltered: (...args: unknown[]) =>
      getScorersByPlayerFiltered(...args),
  },
}));

const getChampionsHistory = vi.fn();

vi.mock('@/modules/champion/service/champion.service', () => ({
  championService: {
    getChampionsHistory: (...args: unknown[]) => getChampionsHistory(...args),
  },
}));

const participation = (
  overrides: Partial<TeamParticipation> = {}
): TeamParticipation => ({
  tournamentId: guid('tournament-1'),
  tournamentName: 'Apertura 2025',
  tournamentSlug: 'apertura-2025',
  category: TournamentCategory.Masculine,
  seasonId: guid('season-1'),
  seasonName: 'Temporada 2025',
  year: 2025,
  isCurrent: true,
  ...overrides,
});

const summary = (overrides: Partial<TeamSummary> = {}): TeamSummary => ({
  divisionId: guid('division-1'),
  divisionName: 'Zona A',
  position: 3,
  totalTeams: 8,
  played: 7,
  wins: 5,
  losses: 2,
  pointsFor: 560,
  pointsAgainst: 540,
  pointsDifference: 20,
  points: 12,
  ...overrides,
});

const finishedMatch = (
  overrides: Partial<TeamMatch> = {}
): TeamMatch => ({
  matchId: guid('match-1'),
  matchDate: '2025-01-01T20:00:00Z',
  isFinished: true,
  status: 'Finished',
  isHome: true,
  opponentTeamId: guid('opp-1'),
  opponentName: 'Los Cuervos',
  opponentLogoUrl: null,
  teamScore: 80,
  opponentScore: 70,
  result: 'W',
  venueName: 'Gimnasio Central',
  ...overrides,
});

const renderPage = () =>
  render(
    <MemoryRouter initialEntries={[`/equipos/${TEAM_ID}`]}>
      <Routes>
        <Route path="/equipos/:teamId" element={<PublicTeamPage />} />
      </Routes>
    </MemoryRouter>
  );

describe('PublicTeamPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    getTeamParticipations.mockResolvedValue({ data: [participation()] });
    getTeamSummary.mockResolvedValue({ data: summary() });
    getTeamMatches.mockResolvedValue({ data: [finishedMatch()] });
    getScorersByPlayerFiltered.mockResolvedValue({ data: { items: [] } });
    getChampionsHistory.mockResolvedValue({ data: [] });
  });

  it('renders the box-score tiles: position from the standing, record/differential from all matches', async () => {
    // Position comes from the standing; record and differential are aggregated
    // from every finished match (2 wins, 1 loss; +30 differential).
    getTeamMatches.mockResolvedValue({
      data: [
        finishedMatch({ matchId: guid('m-1'), result: 'W', teamScore: 80, opponentScore: 70 }),
        finishedMatch({ matchId: guid('m-2'), result: 'W', teamScore: 90, opponentScore: 60 }),
        finishedMatch({ matchId: guid('m-3'), result: 'L', teamScore: 55, opponentScore: 65 }),
      ],
    });

    renderPage();

    await waitFor(() => {
      expect(screen.getByText('Posición')).toBeInTheDocument();
    });

    expect(screen.getByText('3º')).toBeInTheDocument();
    expect(screen.getByText(/de 8/)).toBeInTheDocument();
    expect(screen.getByText('2-1')).toBeInTheDocument();
    expect(screen.getByText('+30')).toBeInTheDocument();
  });

  it('shows a negative differential when points against exceed points for', async () => {
    getTeamMatches.mockResolvedValue({
      data: [finishedMatch({ result: 'L', teamScore: 60, opponentScore: 90 })],
    });

    renderPage();

    await waitFor(() => {
      expect(screen.getByText('-30')).toBeInTheDocument();
    });
  });

  it('shows a quiet empty state when there is no standing and no matches', async () => {
    getTeamSummary.mockResolvedValue({ data: null });
    getTeamMatches.mockResolvedValue({ data: [] });

    renderPage();

    await waitFor(() => {
      expect(
        screen.getByText(/Sin datos para este torneo todavía/i)
      ).toBeInTheDocument();
    });
  });

  it('renders the streak, the opponent fixture and the top scorers', async () => {
    getScorersByPlayerFiltered.mockResolvedValue({
      data: { items: [{ playerId: guid('p-1'), fullName: 'Juan Pérez', points: 42 }] },
    });

    renderPage();

    await waitFor(() => {
      expect(screen.getByText('Los Cuervos')).toBeInTheDocument();
    });

    // Racha pill for the single won match.
    expect(screen.getByLabelText('Victoria')).toBeInTheDocument();
    // Top scorer.
    expect(screen.getByText('Juan Pérez')).toBeInTheDocument();
    expect(screen.getByText('42')).toBeInTheDocument();
  });

  it('renders a tournament selector only when the team has more than one participation', async () => {
    getTeamParticipations.mockResolvedValue({
      data: [
        participation(),
        participation({
          tournamentId: guid('tournament-2'),
          tournamentName: 'Clausura 2024',
          seasonName: 'Temporada 2024',
          isCurrent: false,
        }),
      ],
    });

    renderPage();

    await waitFor(() => {
      expect(screen.getByLabelText('Torneo')).toBeInTheDocument();
    });
  });

  it('renders the titles section only when the team has won something', async () => {
    getChampionsHistory.mockResolvedValue({
      data: [
        {
          tournamentId: guid('tournament-1'),
          tournamentName: 'Apertura 2025',
          seasonName: 'Temporada 2025',
          category: TournamentCategory.Masculine,
          divisionName: 'Zona A',
          championTeam: {
            teamId: guid(TEAM_ID),
            teamName: 'Los Halcones',
            logoUrl: null,
          },
        },
      ],
    });

    renderPage();

    const titles = await screen.findByRole('heading', { name: 'Títulos' });
    const section = titles.closest('section');
    expect(section).not.toBeNull();
    expect(
      within(section as HTMLElement).getByText(/Apertura 2025 · Zona A/)
    ).toBeInTheDocument();
  });
});
