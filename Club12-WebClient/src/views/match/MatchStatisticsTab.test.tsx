import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import MatchStatisticsTab from '@/views/match/MatchStatisticsTab';
import { useMatch } from '@/modules/match/hook/match.hook';
import { GUID } from '@/modules/core/types/types';
import { IMatchResponse } from '@/modules/match/type/match';

const getPlayerStatisticsByFilter = vi.fn(() =>
  Promise.resolve({ items: [], totalCount: 0, pageNumber: 1, pageSize: 300 })
);

vi.mock('@/modules/playerStatistic/hook/playerStatistic.hook', () => ({
  usePlayerStatistic: () => ({
    getPlayerStatisticsByFilter,
  }),
}));

const loadMatchResultFromSheets = vi.fn(() =>
  Promise.resolve({ id: 'match-1' })
);

vi.mock('@/modules/match/hook/match.hook');
const mockedUseMatch = vi.mocked(useMatch);

vi.mock('@/modules/core/utils/confirmDialog', () => ({
  notifySuccess: vi.fn(() => Promise.resolve()),
}));

const guid = (value: string) => value as GUID;

const HOME_TEAM_ID = guid('1111-1111-1111-1111-1111');
const VISITOR_TEAM_ID = guid('3333-3333-3333-3333-3333');
const HOME_PLAYER_ID = guid('2222-2222-2222-2222-2222');
const VISITOR_PLAYER_ID = guid('4444-4444-4444-4444-4444');

const buildMatch = (
  overrides: {
    visitorPlayers?: unknown[];
  } = {}
): IMatchResponse =>
  ({
    id: guid('aaaa-aaaa-aaaa-aaaa-aaaa'),
    matchDate: '2026-01-01T00:00:00Z',
    matchType: 'Regular',
    slug: 'match-1',
    isFinished: false,
    winningTeamId: null,
    winningTeamName: null,
    venue: null,
    stageId: null,
    status: null,
    homeTeam: {
      id: HOME_TEAM_ID,
      name: 'Local FC',
      logoUrl: '',
      score: 0,
      scorers: [],
      players: [
        {
          id: HOME_PLAYER_ID,
          firstName: 'Juan',
          secondName: '',
          lastName: 'Perez',
          fullName: 'Juan Perez',
          teamId: HOME_TEAM_ID,
        },
      ],
    },
    visitorTeam: {
      id: VISITOR_TEAM_ID,
      name: 'Visitante FC',
      logoUrl: '',
      score: 0,
      scorers: [],
      players:
        overrides.visitorPlayers ??
        [
          {
            id: VISITOR_PLAYER_ID,
            firstName: 'Ana',
            secondName: '',
            lastName: 'Gomez',
            fullName: 'Ana Gomez',
            teamId: VISITOR_TEAM_ID,
          },
        ],
    },
  }) as unknown as IMatchResponse;

beforeEach(() => {
  vi.clearAllMocks();
  getPlayerStatisticsByFilter.mockResolvedValue({
    items: [],
    totalCount: 0,
    pageNumber: 1,
    pageSize: 300,
  });
  mockedUseMatch.mockReturnValue({
    loadMatchResultFromSheets,
  } as unknown as ReturnType<typeof useMatch>);
});

describe('MatchStatisticsTab — result derived from both teams\' sheets', () => {
  it('blocks saving while both teams sum to the same score (a tie)', async () => {
    render(<MatchStatisticsTab match={buildMatch()} />);

    await screen.findByLabelText('Puntos de Juan Perez');

    expect(
      screen.getByText(/no se permiten empates/i)
    ).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: 'Guardar resultado' })
    ).toBeDisabled();
  });

  it('saves the result once the sums differ, calling loadMatchResultFromSheets with both sheets', async () => {
    render(<MatchStatisticsTab match={buildMatch()} />);

    const homeInput = await screen.findByLabelText('Puntos de Juan Perez');
    const visitorInput = await screen.findByLabelText('Puntos de Ana Gomez');

    fireEvent.change(homeInput, { target: { value: '55' } });
    fireEvent.change(visitorInput, { target: { value: '40' } });

    const saveButton = await screen.findByRole('button', {
      name: 'Guardar resultado',
    });
    await waitFor(() => expect(saveButton).toBeEnabled());

    fireEvent.click(saveButton);

    await waitFor(() =>
      expect(loadMatchResultFromSheets).toHaveBeenCalledWith('aaaa-aaaa-aaaa-aaaa-aaaa', {
        homeScores: [{ playerId: HOME_PLAYER_ID, points: 55 }],
        visitorScores: [{ playerId: VISITOR_PLAYER_ID, points: 40 }],
      })
    );
  });

  it('disables saving when either team has no registered players', async () => {
    render(<MatchStatisticsTab match={buildMatch({ visitorPlayers: [] })} />);

    await screen.findByText(/sin jugadores registrados/i);

    expect(
      screen.getByText(/ambos equipos necesitan jugadores/i)
    ).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: 'Guardar resultado' })
    ).toBeDisabled();
  });

  it('pre-fills the forms from already-loaded Points statistics', async () => {
    getPlayerStatisticsByFilter.mockResolvedValue({
      items: [
        {
          id: 'stat-1',
          matchId: 'aaaa-aaaa-aaaa-aaaa-aaaa',
          playerId: HOME_PLAYER_ID,
          type: 'Points',
          value: 20,
        },
      ],
      totalCount: 1,
      pageNumber: 1,
      pageSize: 300,
    });

    render(<MatchStatisticsTab match={buildMatch()} />);

    const homeInput = (await screen.findByLabelText(
      'Puntos de Juan Perez'
    )) as HTMLInputElement;

    expect(homeInput.value).toBe('20');
  });
});
