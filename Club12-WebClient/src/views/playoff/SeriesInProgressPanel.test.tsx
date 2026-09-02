import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import SeriesInProgressPanel from '@/views/playoff/SeriesInProgressPanel';
import { matchSeriesService } from '@/modules/matchSeries/service/matchSeries.service';
import type { IMatchSeriesResponse } from '@/modules/matchSeries/type/matchSeries.d';
import type { GUID } from '@/modules/core/types/types';

vi.mock('@/modules/matchSeries/service/matchSeries.service');
vi.mock('@/modules/core/utils/confirmDialog', () => ({
  notifyError: vi.fn(),
  notifySuccess: vi.fn(),
}));

const mockedAddGameToSeries = vi.mocked(matchSeriesService.addGameToSeries);

const SERIES_ID = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa' as GUID;
const OTHER_SERIES_ID = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb' as GUID;

const buildSeries = (
  overrides: Partial<IMatchSeriesResponse> = {}
): IMatchSeriesResponse => ({
  id: SERIES_ID,
  stageId: 'cccccccc-cccc-cccc-cccc-cccccccccccc' as GUID,
  homeTeamId: 'dddddddd-dddd-dddd-dddd-dddddddddddd' as GUID,
  homeTeamName: 'Halcones',
  visitorTeamId: 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee' as GUID,
  visitorTeamName: 'Cóndores',
  bestOf: 3,
  winningTeamId: null,
  winningTeamName: null,
  games: [],
  ...overrides,
});

afterEach(() => {
  vi.clearAllMocks();
});

describe('SeriesInProgressPanel', () => {
  it('renders nothing when there are no undecided series', () => {
    const decided = buildSeries({ winningTeamId: 'dddddddd-dddd-dddd-dddd-dddddddddddd' as GUID });
    const { container } = render(
      <SeriesInProgressPanel
        seriesById={new Map([[decided.id, decided]])}
        onGameAdded={vi.fn()}
      />
    );

    expect(container).toBeEmptyDOMElement();
  });

  it('lists undecided series with their played-games summary', () => {
    const series = buildSeries({
      games: [
        {
          id: 'ffffffff-ffff-ffff-ffff-ffffffffffff' as GUID,
          slug: 'game-1',
          gameNumber: 1,
          isFinished: true,
          homeScore: 78,
          visitorScore: 70,
        } as unknown as IMatchSeriesResponse['games'][number],
      ],
    });

    render(
      <SeriesInProgressPanel
        seriesById={new Map([[series.id, series]])}
        onGameAdded={vi.fn()}
      />
    );

    expect(screen.getByText(/Halcones vs Cóndores — al mejor de 3/)).toBeInTheDocument();
    expect(screen.getByText('J1 78-70')).toBeInTheDocument();
  });

  it('disables "Agregar próximo partido" once every game has been played', () => {
    const series = buildSeries({
      bestOf: 1,
      games: [
        {
          id: 'ffffffff-ffff-ffff-ffff-ffffffffffff' as GUID,
          slug: 'game-1',
          gameNumber: 1,
          isFinished: true,
          homeScore: 78,
          visitorScore: 70,
        } as unknown as IMatchSeriesResponse['games'][number],
      ],
    });

    render(
      <SeriesInProgressPanel
        seriesById={new Map([[series.id, series]])}
        onGameAdded={vi.fn()}
      />
    );

    expect(
      screen.getByRole('button', { name: 'Agregar próximo partido' })
    ).toBeDisabled();
  });

  it('adds the next game and calls onGameAdded on confirm', async () => {
    mockedAddGameToSeries.mockResolvedValue({} as never);
    const onGameAdded = vi.fn();
    const series = buildSeries();

    render(
      <SeriesInProgressPanel
        seriesById={new Map([[series.id, series]])}
        onGameAdded={onGameAdded}
      />
    );

    fireEvent.click(screen.getByRole('button', { name: 'Agregar próximo partido' }));

    const dateInput = await screen.findByLabelText(/Fecha y hora/);
    fireEvent.change(dateInput, { target: { value: '2026-10-05T18:00' } });

    fireEvent.click(screen.getByRole('button', { name: 'Agregar' }));

    await waitFor(() => expect(mockedAddGameToSeries).toHaveBeenCalledTimes(1));
    expect(mockedAddGameToSeries).toHaveBeenCalledWith(
      series.id,
      expect.objectContaining({ matchDate: expect.any(String) })
    );
    await waitFor(() => expect(onGameAdded).toHaveBeenCalled());
  });

  it('ignores an already-decided series while listing an undecided one', () => {
    const decided = buildSeries({
      id: OTHER_SERIES_ID,
      winningTeamId: 'dddddddd-dddd-dddd-dddd-dddddddddddd' as GUID,
    });
    const undecided = buildSeries();

    render(
      <SeriesInProgressPanel
        seriesById={
          new Map([
            [decided.id, decided],
            [undecided.id, undecided],
          ])
        }
        onGameAdded={vi.fn()}
      />
    );

    expect(
      screen.getAllByRole('button', { name: 'Agregar próximo partido' })
    ).toHaveLength(1);
  });
});
