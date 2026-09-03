import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import StatisticsPage from '@/views/panel/StatisticsPage';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { useTeam } from '@/modules/team/hook/team.hook';
import { useMatch } from '@/modules/match/hook/match.hook';
import { useScorer } from '@/modules/scorer/hook/scorer.hook';
import { useSeason } from '@/modules/season/hook/season.hook';
import { usePlayerSanction } from '@/modules/playerSanction/hook/playerSanction.hook';
import { GUID } from '@/modules/core/types/types';

vi.mock('@/modules/tournament/hook/tournament.hook');
vi.mock('@/modules/team/hook/team.hook');
vi.mock('@/modules/match/hook/match.hook');
vi.mock('@/modules/scorer/hook/scorer.hook');
vi.mock('@/modules/season/hook/season.hook');
vi.mock('@/modules/playerSanction/hook/playerSanction.hook');

const mockedUseTournament = vi.mocked(useTournament);
const mockedUseTeam = vi.mocked(useTeam);
const mockedUseMatch = vi.mocked(useMatch);
const mockedUseScorer = vi.mocked(useScorer);
const mockedUseSeason = vi.mocked(useSeason);
const mockedUsePlayerSanction = vi.mocked(usePlayerSanction);

const tournamentA = '11111111-1111-1111-1111-111111111111' as GUID;
const tournamentB = '22222222-2222-2222-2222-222222222222' as GUID;
const seasonA = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa' as GUID;

const setupHooks = () => {
  const getAllTournamentsByFilter = vi.fn().mockResolvedValue({
    items: [
      { id: tournamentA, name: 'Apertura', status: 'Ongoing' },
      { id: tournamentB, name: 'Clausura', status: 'Finished' },
    ],
    totalCount: 2,
  });
  const getTeamsByFiltered = vi.fn().mockResolvedValue({ totalCount: 10 });
  const getMatchByFilter = vi.fn().mockResolvedValue({ totalCount: 3 });
  const getScorersByPlayerFiltered = vi.fn().mockResolvedValue({ items: [] });
  const getSeasonsByFiltered = vi.fn().mockResolvedValue([
    {
      id: seasonA,
      name: 'Temporada A',
      year: 2026,
      tournaments: [{ id: tournamentA, name: 'Apertura' }],
    },
  ]);
  const getPlayerSanctionByFilter = vi.fn().mockResolvedValue({ totalCount: 1 });

  mockedUseTournament.mockReturnValue({
    tournaments: [
      { id: tournamentA, name: 'Apertura', status: 'Ongoing' },
      { id: tournamentB, name: 'Clausura', status: 'Finished' },
    ],
    getAllTournamentsByFilter,
  } as unknown as ReturnType<typeof useTournament>);
  mockedUseTeam.mockReturnValue({
    getTeamsByFiltered,
  } as unknown as ReturnType<typeof useTeam>);
  mockedUseMatch.mockReturnValue({
    getMatchByFilter,
  } as unknown as ReturnType<typeof useMatch>);
  mockedUseScorer.mockReturnValue({
    getScorersByPlayerFiltered,
  } as unknown as ReturnType<typeof useScorer>);
  mockedUseSeason.mockReturnValue({
    seasons: [
      {
        id: seasonA,
        name: 'Temporada A',
        year: 2026,
        tournaments: [{ id: tournamentA, name: 'Apertura' }],
      },
    ],
    getSeasonsByFiltered,
  } as unknown as ReturnType<typeof useSeason>);
  mockedUsePlayerSanction.mockReturnValue({
    getPlayerSanctionByFilter,
  } as unknown as ReturnType<typeof usePlayerSanction>);

  return { getTeamsByFiltered, getMatchByFilter, getPlayerSanctionByFilter };
};

describe('StatisticsPage — filter bar UX', () => {
  it('shows the default option in both scope selects', async () => {
    setupHooks();
    render(<StatisticsPage />);

    await screen.findByText('Torneos');

    expect(
      screen.getByRole('combobox', { name: 'Temporada' })
    ).toHaveTextContent('Todas');
    expect(screen.getByRole('combobox', { name: 'Torneo' })).toHaveTextContent(
      'Todos'
    );
  });

  it('keeps the filter bar mounted while a refilter is loading', async () => {
    const { getTeamsByFiltered } = setupHooks();
    const user = userEvent.setup();
    render(<StatisticsPage />);

    await screen.findByText('Torneos');

    // The summary refetch triggered by the tournament pick never resolves.
    let releaseRefilter: () => void = () => {};
    getTeamsByFiltered.mockImplementation(
      () =>
        new Promise(resolve => {
          releaseRefilter = () => resolve({ totalCount: 5 });
        })
    );

    const select = screen.getByRole('combobox', { name: 'Torneo' });
    await user.click(select);
    const listbox = await screen.findByRole('listbox');
    await user.click(within(listbox).getByText('Apertura'));

    // Only the stats content reloads — the filter bar stays put.
    expect(
      screen.getByRole('combobox', { name: 'Temporada' })
    ).toBeInTheDocument();
    expect(
      screen.getByRole('combobox', { name: 'Torneo' })
    ).toBeInTheDocument();

    releaseRefilter();
  });
});

describe('StatisticsPage — Torneo filter scoping', () => {
  it('fetches unscoped (global) counts when no torneo/temporada is selected', async () => {
    const { getTeamsByFiltered } = setupHooks();
    render(<StatisticsPage />);

    await waitFor(() => expect(getTeamsByFiltered).toHaveBeenCalled());
    expect(getTeamsByFiltered).toHaveBeenCalledWith({ pageSize: 1, pageNumber: 1 });
  });

  it('scopes every summary card to the chosen torneo, not just goleadores', async () => {
    const { getTeamsByFiltered, getMatchByFilter, getPlayerSanctionByFilter } =
      setupHooks();
    const user = userEvent.setup();
    render(<StatisticsPage />);

    await screen.findByText('Torneos');
    getTeamsByFiltered.mockClear();
    getMatchByFilter.mockClear();
    getPlayerSanctionByFilter.mockClear();

    const select = await screen.findByRole('combobox', { name: 'Torneo' });
    await user.click(select);
    const listbox = await screen.findByRole('listbox');
    await user.click(within(listbox).getByText('Apertura'));

    await waitFor(() =>
      expect(getTeamsByFiltered).toHaveBeenCalledWith({
        tournamentId: tournamentA,
        pageSize: 1,
        pageNumber: 1,
      })
    );
    expect(getMatchByFilter).toHaveBeenCalledWith({
      tournamentId: tournamentA,
      pageSize: 1,
      pageNumber: 1,
      isFinished: true,
    });
    expect(getPlayerSanctionByFilter).toHaveBeenCalledWith({
      tournamentId: tournamentA,
      pageSize: 1,
      pageNumber: 1,
    });

    // Torneos card now reflects the single selected tournament, not the
    // club-wide total of 2.
    await waitFor(() => {
      const label = screen.getByText('Torneos');
      const card = label.closest('.MuiCard-root') as HTMLElement;
      expect(within(card).getByText('1')).toBeInTheDocument();
    });
  });
});
