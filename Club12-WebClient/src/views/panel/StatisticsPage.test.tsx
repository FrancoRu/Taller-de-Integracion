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

/**
 * A fresh array literal every call — matching what the real API client
 * actually returns (a new response object per request), unlike
 * `mockResolvedValue`'s single captured value reused across every call.
 * The infinite-loop regression below only reproduces with this shape: a
 * mock returning the exact same reference every time would hide it.
 */
const freshSeasonsResponse = () => [
  {
    id: seasonA,
    name: 'Temporada A',
    year: 2026,
    tournaments: [{ id: tournamentA, name: 'Apertura' }],
  },
];

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
  // Mirrors real React state: `seasons` only gets a new reference when
  // getSeasonsByFiltered actually resolves (one real setSeasons call), not
  // on every unrelated render — unlike mockResolvedValue's single reused
  // value, this still gives a genuinely NEW array object each time, since
  // that's what the real API client returns per request.
  let currentSeasons = freshSeasonsResponse();
  const getSeasonsByFiltered = vi.fn().mockImplementation(() => {
    currentSeasons = freshSeasonsResponse();
    return Promise.resolve(currentSeasons);
  });
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
  // Reads the same captured `currentSeasons` reference on every render —
  // it only changes when getSeasonsByFiltered above reassigns it, exactly
  // like the real SeasonProvider only re-renders consumers with a new
  // `seasons` array when setSeasons() actually runs.
  mockedUseSeason.mockImplementation(
    () =>
      ({
        seasons: currentSeasons,
        getSeasonsByFiltered,
      }) as unknown as ReturnType<typeof useSeason>
  );
  mockedUsePlayerSanction.mockReturnValue({
    getPlayerSanctionByFilter,
  } as unknown as ReturnType<typeof usePlayerSanction>);

  return {
    getTeamsByFiltered,
    getMatchByFilter,
    getPlayerSanctionByFilter,
    getSeasonsByFiltered,
  };
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

describe('StatisticsPage — Temporada filter does not loop forever', () => {
  it('fetches seasons exactly once, never as part of the scope-triggered reload', async () => {
    // Regression test for an infinite-loop bug: scopeTournamentIds derives
    // from `seasons` via season.tournaments.map(...) — a brand-new array
    // every time it recomputes. The summary effect used to refetch seasons
    // itself on every run, which (via setSeasons) handed the component a
    // fresh `seasons` reference — making scopeTournamentIds "change" after
    // every single load and retriggering the same effect forever, the
    // moment a temporada/torneo filter was active (unscoped stayed stable
    // since scopeTournamentIds is just `null` either way there). Seasons
    // must now be fetched once, decoupled from that reload entirely — this
    // asserts the call count directly rather than racing a real loop, which
    // depends on React's own runaway-render guard firing within the test's
    // timing window instead of failing deterministically.
    const { getTeamsByFiltered, getSeasonsByFiltered } = setupHooks();
    const user = userEvent.setup();
    render(<StatisticsPage />);

    await screen.findByText('Torneos');
    expect(getSeasonsByFiltered).toHaveBeenCalledTimes(1);

    const select = await screen.findByRole('combobox', { name: 'Temporada' });
    await user.click(select);
    const listbox = await screen.findByRole('listbox');
    await user.click(within(listbox).getByText('Temporada A'));

    await waitFor(() =>
      expect(getTeamsByFiltered).toHaveBeenCalledWith({
        tournamentId: tournamentA,
        pageSize: 1,
        pageNumber: 1,
      })
    );

    expect(getSeasonsByFiltered).toHaveBeenCalledTimes(1);
  });
});
