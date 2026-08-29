import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';
import StageMatchesByRound from '@/views/match/StageMatchesByRound';
import { useMatch } from '@/modules/match/hook/match.hook';
import { IMatchResponse, IRoundMatchesResponse } from '@/modules/match/type/match';
import type { GUID } from '@/modules/core/types/types';

vi.mock('@/modules/match/hook/match.hook');

const mockedUseMatch = vi.mocked(useMatch);

const STAGE_ID = 'stage-1' as unknown as GUID;

const buildMatch = (): IMatchResponse =>
  ({
    id: 'match-1' as unknown as GUID,
    slug: 'match-1',
    matchDate: '2026-01-01T18:00:00Z',
    isFinished: false,
    status: null,
    homeTeam: { id: 'home' as unknown as GUID, name: 'Local FC', logoUrl: '', score: 0 },
    visitorTeam: { id: 'away' as unknown as GUID, name: 'Visita FC', logoUrl: '', score: 0 },
  }) as unknown as IMatchResponse;

const buildRounds = (): IRoundMatchesResponse[] =>
  [{ round: 1, matches: [buildMatch()] }] as unknown as IRoundMatchesResponse[];

const setup = () => {
  mockedUseMatch.mockReturnValue({
    getStageMatchesByRound: vi.fn().mockResolvedValue(buildRounds()),
    suspendMatch: vi.fn().mockResolvedValue(true),
  } as unknown as ReturnType<typeof useMatch>);
};

const renderComponent = () =>
  render(
    <MemoryRouter initialEntries={['/panel/fases/fase-1']}>
      <Routes>
        <Route
          path="/panel/fases/:stageId"
          element={<StageMatchesByRound stageId={STAGE_ID} />}
        />
        <Route
          path="/panel/partidos/:matchId"
          element={<div>detalle-partido</div>}
        />
      </Routes>
    </MemoryRouter>
  );

afterEach(() => {
  vi.clearAllMocks();
});

describe('StageMatchesByRound — admin match detail entry (QA wave 1)', () => {
  it('links each match to its detail page so results can be loaded', async () => {
    setup();
    renderComponent();

    const button = await screen.findByRole('button', {
      name: 'Cargar resultado',
    });
    await userEvent.click(button);

    await waitFor(() =>
      expect(screen.getByText('detalle-partido')).toBeInTheDocument()
    );
  });
});
