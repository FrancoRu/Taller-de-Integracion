import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import MatchStatisticsTab from '@/views/match/MatchStatisticsTab';
import { GUID } from '@/modules/core/types/types';
import { IMatchResponse } from '@/modules/match/type/match';

const getPlayerStatisticsByFilter = vi.fn(() =>
  Promise.resolve({ items: [], totalCount: 0, pageNumber: 1, pageSize: 300 })
);
const loadMatchSheet = vi.fn(() => Promise.resolve([{ id: 'x' }]));

vi.mock('@/modules/playerStatistic/hook/playerStatistic.hook', () => ({
  usePlayerStatistic: () => ({
    getPlayerStatisticsByFilter,
    loadMatchSheet,
  }),
}));

vi.mock('@/modules/core/utils/confirmDialog', () => ({
  notifySuccess: vi.fn(() => Promise.resolve()),
}));

const guid = (value: string) => value as GUID;

const HOME_TEAM_ID = guid('1111-1111-1111-1111-1111');
const PLAYER_ID = guid('2222-2222-2222-2222-2222');

const buildMatch = (): IMatchResponse =>
  ({
    id: guid('aaaa-aaaa-aaaa-aaaa-aaaa'),
    matchDate: '2026-01-01T00:00:00Z',
    matchType: 'Regular',
    slug: 'match-1',
    isFinished: true,
    winningTeamId: HOME_TEAM_ID,
    winningTeamName: 'Local',
    venue: null,
    stageId: null,
    status: null,
    homeTeam: {
      id: HOME_TEAM_ID,
      name: 'Local FC',
      logoUrl: '',
      score: 50,
      scorers: [],
      players: [
        {
          id: PLAYER_ID,
          firstName: 'Juan',
          secondName: '',
          lastName: 'Perez',
          fullName: 'Juan Perez',
          teamId: HOME_TEAM_ID,
        },
      ],
    },
    visitorTeam: {
      id: guid('3333-3333-3333-3333-3333'),
      name: 'Visitante FC',
      logoUrl: '',
      score: 0,
      scorers: [],
      players: [],
    },
  }) as unknown as IMatchResponse;

describe('MatchStatisticsTab match-sheet', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('blocks submit while the sum does not match the score, then loads the sheet', async () => {
    render(<MatchStatisticsTab match={buildMatch()} />);

    // Wait for the initial statistics load to finish.
    const loadButtons = await screen.findAllByRole('button', {
      name: 'Cargar planilla',
    });
    // The home team (index 0) has players; open its sheet dialog.
    fireEvent.click(loadButtons[0]);

    const saveButton = await screen.findByRole('button', {
      name: 'Guardar planilla',
    });

    // Sum starts at 0 but the score is 50 -> submit is blocked.
    expect(saveButton).toBeDisabled();
    expect(screen.getByText(/Faltan 50 puntos/)).toBeInTheDocument();

    // Enter the matching points -> submit is enabled.
    const pointsInput = screen.getByLabelText('Puntos de Juan Perez');
    fireEvent.change(pointsInput, { target: { value: '50' } });

    await waitFor(() => expect(saveButton).toBeEnabled());

    fireEvent.click(saveButton);

    await waitFor(() =>
      expect(loadMatchSheet).toHaveBeenCalledWith({
        matchId: 'aaaa-aaaa-aaaa-aaaa-aaaa',
        teamId: HOME_TEAM_ID,
        scores: [{ playerId: PLAYER_ID, points: 50 }],
      })
    );
  });
});
