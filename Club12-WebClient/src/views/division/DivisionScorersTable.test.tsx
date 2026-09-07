import { render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { GUID } from '@/modules/core/types/types';
import { IScorerByPlayerResponse } from '@/modules/scorer/type/scorer.d';
import { FILTER_OPTIONS_PAGE_SIZE } from '@/modules/core/constants/pagination';
import DivisionScorersTable from './DivisionScorersTable';

const guid = (value: string) => value as GUID;

const getScorersByPlayerFiltered = vi.fn();

vi.mock('@/modules/scorer/service/scorer.service', () => ({
  scorerService: {
    getScorersByPlayerFiltered: (...args: unknown[]) =>
      getScorersByPlayerFiltered(...args),
  },
}));

const scorer = (name: string, points: number): IScorerByPlayerResponse => ({
  playerId: guid(`player-${name}`),
  fullName: name,
  points,
  teamName: 'Equipo',
});

describe('DivisionScorersTable', () => {
  it('fetches the full ranking page size when no limit is given (admin panel)', async () => {
    getScorersByPlayerFiltered.mockResolvedValue({
      data: { items: [scorer('Ana', 50)] },
    });

    render(<DivisionScorersTable divisionId={guid('division-1')} />);

    await waitFor(() => expect(screen.getByText('Ana')).toBeInTheDocument());

    expect(getScorersByPlayerFiltered).toHaveBeenCalledWith(
      expect.objectContaining({ pageSize: FILTER_OPTIONS_PAGE_SIZE })
    );
  });

  it('caps the fetched page size to the given limit (public tournament page)', async () => {
    getScorersByPlayerFiltered.mockResolvedValue({
      data: { items: [scorer('Ana', 50)] },
    });

    render(<DivisionScorersTable divisionId={guid('division-1')} limit={10} />);

    await waitFor(() => expect(screen.getByText('Ana')).toBeInTheDocument());

    expect(getScorersByPlayerFiltered).toHaveBeenCalledWith(
      expect.objectContaining({ pageSize: 10 })
    );
  });

  it('shows the empty state, not a wall of zeroes, when nobody has actually scored', async () => {
    // The backend intentionally lists every registered player defaulting to
    // 0 (other consumers rely on that) — the UI must not mistake that for a
    // real ranking.
    getScorersByPlayerFiltered.mockResolvedValue({
      data: { items: [scorer('Ana', 0), scorer('Beto', 0), scorer('Caro', 0)] },
    });

    render(<DivisionScorersTable divisionId={guid('division-1')} />);

    expect(
      await screen.findByText('Todavía no hay goleadores registrados en esta división.')
    ).toBeInTheDocument();
    expect(screen.queryByText('Ana')).not.toBeInTheDocument();
  });

  it('shows the ranking table when at least one real scorer is present, even mixed with zeroes', async () => {
    getScorersByPlayerFiltered.mockResolvedValue({
      data: { items: [scorer('Ana', 3), scorer('Beto', 0), scorer('Caro', 0)] },
    });

    render(<DivisionScorersTable divisionId={guid('division-1')} />);

    expect(await screen.findByText('Ana')).toBeInTheDocument();
    expect(screen.getByText('Beto')).toBeInTheDocument();
    expect(
      screen.queryByText('Todavía no hay goleadores registrados en esta división.')
    ).not.toBeInTheDocument();
  });
});
