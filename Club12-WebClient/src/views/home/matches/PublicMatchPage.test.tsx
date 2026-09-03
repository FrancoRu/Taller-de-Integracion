import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { GUID } from '@/modules/core/types/types';
import { MatchStatus } from '@/modules/core/enum/match/matchStatus';
import { IMatchResponse } from '@/modules/match/type/match';
import { ITeamMatchResponse } from '@/modules/team/type/team';
import PublicMatchPage from '@/views/home/matches/PublicMatchPage';

const guid = (value: string) => value as GUID;

const HOME_ID = guid('home-1');
const VISITOR_ID = guid('visitor-1');

// A mutable holder so each test can swap the match the mocked `useMatch`
// returns, while keeping the hook's callback references stable across renders.
const { state, getMatchByIdMock, getPlayerSanctionByFilterMock } = vi.hoisted(
  () => ({
    state: { match: null as IMatchResponse | null },
    getMatchByIdMock: vi.fn().mockResolvedValue(undefined),
    getPlayerSanctionByFilterMock: vi.fn().mockResolvedValue(undefined),
  })
);

vi.mock('@/modules/match/hook/match.hook', () => ({
  useMatch: () => ({ match: state.match, getMatchById: getMatchByIdMock }),
}));

vi.mock('@/modules/playerSanction/hook/playerSanction.hook', () => ({
  usePlayerSanction: () => ({
    playerSanctions: [],
    getPlayerSanctionByFilter: getPlayerSanctionByFilterMock,
  }),
}));

const team = (
  overrides: Partial<ITeamMatchResponse> = {}
): ITeamMatchResponse => ({
  id: HOME_ID,
  name: 'Los Halcones',
  logoUrl: '',
  score: 0,
  players: [],
  scorers: [],
  ...overrides,
});

const match = (overrides: Partial<IMatchResponse> = {}): IMatchResponse =>
  ({
    id: 'match-1' as GUID,
    slug: 'match-1',
    matchDate: '2025-01-01T20:00:00Z',
    matchType: 'Regular',
    isFinished: true,
    winningTeamId: HOME_ID,
    winningTeamName: 'Los Halcones',
    stageId: null,
    status: MatchStatus.Played,
    venue: { id: guid('venue-1'), name: 'Gimnasio Central' },
    homeTeam: team({
      id: HOME_ID,
      name: 'Los Halcones',
      score: 80,
      scorers: [{ playerId: guid('p-1'), fullName: 'Juan Pérez', points: 42 }],
    }),
    visitorTeam: team({
      id: VISITOR_ID,
      name: 'Los Cuervos',
      score: 70,
      scorers: [],
    }),
    ...overrides,
  }) as IMatchResponse;

const renderPage = (
  options: { initialEntries?: string[]; initialIndex?: number } = {}
) =>
  render(
    <MemoryRouter
      initialEntries={options.initialEntries ?? ['/match-1']}
      initialIndex={options.initialIndex}
    >
      <Routes>
        <Route path="/:matchId" element={<PublicMatchPage />} />
        <Route path="/temporadas" element={<div>listado-temporadas</div>} />
        <Route
          path="/torneos/:tournamentId"
          element={<div>detalle-torneo</div>}
        />
      </Routes>
    </MemoryRouter>
  );

describe('PublicMatchPage scoreboard', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    state.match = match();
  });

  it('renders a finished scoreboard with both teams and both scores', async () => {
    renderPage();

    await waitFor(() => {
      // Each team name appears twice: the scoreboard and its scorers card.
      expect(screen.getAllByText('Los Halcones').length).toBeGreaterThan(0);
    });

    expect(screen.getAllByText('Los Cuervos').length).toBeGreaterThan(0);
    expect(screen.getByText('80')).toBeInTheDocument();
    expect(screen.getByText('70')).toBeInTheDocument();
    // The shared status chip labels a played match "Jugado".
    expect(screen.getByText('Jugado')).toBeInTheDocument();
    expect(screen.getByText('Gimnasio Central')).toBeInTheDocument();
  });

  it('shows the date, time and venue as separate icon-led pieces, not one combined string', async () => {
    // Regression: date/time used to render as one Typography whose
    // variant/weight/color flipped depending on isFinished, so a scheduled
    // match's header looked like a different component from a played
    // one's. Both now render through the same markup regardless of status.
    renderPage();

    await waitFor(() => {
      expect(screen.getByText('Jugado')).toBeInTheDocument();
    });

    expect(screen.getByText(/miércoles, 1 de enero de 2025/i)).toBeInTheDocument();
    expect(screen.getByText('17:00')).toBeInTheDocument();
  });

  it('lists each team\'s scorers with points and an empty state when there are none', async () => {
    renderPage();

    await waitFor(() => {
      expect(screen.getByText('Juan Pérez')).toBeInTheDocument();
    });

    expect(screen.getByText('42')).toBeInTheDocument();
    // The visitor team has no scorers loaded.
    expect(screen.getByText('Sin goleadores cargados.')).toBeInTheDocument();
  });

  it('shows a VS placeholder and no scores for a scheduled match', async () => {
    state.match = match({
      isFinished: false,
      winningTeamId: null,
      winningTeamName: null,
      status: MatchStatus.Scheduled,
      homeTeam: team({ id: HOME_ID, name: 'Los Halcones', score: 0 }),
      visitorTeam: team({ id: VISITOR_ID, name: 'Los Cuervos', score: 0 }),
    });

    renderPage();

    await waitFor(() => {
      expect(screen.getByText('VS')).toBeInTheDocument();
    });

    expect(screen.getByText('Programado')).toBeInTheDocument();
    // Regression: a not-yet-played match has no scorers to show — the
    // section used to render regardless of status, showing an empty
    // "Sin goleadores cargados." card for a match that hasn't happened yet.
    expect(screen.queryByText('Goleadores del partido')).not.toBeInTheDocument();
    expect(screen.queryByText('Sin goleadores cargados.')).not.toBeInTheDocument();
  });

  it('sorts scorers by points descending', async () => {
    state.match = match({
      homeTeam: team({
        id: HOME_ID,
        name: 'Los Halcones',
        score: 80,
        scorers: [
          { playerId: guid('p-1'), fullName: 'Ana', points: 10 },
          { playerId: guid('p-2'), fullName: 'Beto', points: 30 },
        ],
      }),
    });

    renderPage();

    const list = await screen.findByRole('list');
    const names = within(list)
      .getAllByRole('listitem')
      .map(item => item.textContent);

    expect(names[0]).toContain('Beto');
    expect(names[1]).toContain('Ana');
  });
});

describe('PublicMatchPage — "Volver" target', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('goes back via real browser history, landing on the tournament page it actually came from', async () => {
    state.match = match({ tournamentId: guid('tournament-1') });
    // A real prior history entry — "Volver" is real browser-history back,
    // not a reconstructed URL that would always land on the tournament's
    // default tab regardless of where "here" actually was.
    renderPage({
      initialEntries: ['/torneos/tournament-1', '/match-1'],
      initialIndex: 1,
    });

    const back = await screen.findByRole('button', { name: /Volver al torneo/ });
    await userEvent.click(back);

    expect(screen.getByText('detalle-torneo')).toBeInTheDocument();
  });

  it('labels the button "Volver a temporadas" when the match has no tournament resolved', async () => {
    state.match = match({ tournamentId: null });
    renderPage({ initialEntries: ['/temporadas', '/match-1'], initialIndex: 1 });

    const back = await screen.findByRole('button', { name: /Volver a temporadas/ });
    await userEvent.click(back);

    expect(screen.getByText('listado-temporadas')).toBeInTheDocument();
  });
});
