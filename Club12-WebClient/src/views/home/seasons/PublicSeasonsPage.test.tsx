import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import PublicSeasonsPage from '@/views/home/seasons/PublicSeasonsPage';
import { useSeason } from '@/modules/season/hook/season.hook';

vi.mock('@/modules/season/hook/season.hook');

const mockedUseSeason = vi.mocked(useSeason);

const renderPage = () =>
  render(
    <MemoryRouter>
      <PublicSeasonsPage />
    </MemoryRouter>
  );

describe('PublicSeasonsPage — initial load failure', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows a quiet inline error with a retry (no blocking alert) when the initial GET fails', async () => {
    // A failed GET returns void — the page must render the inline retry state
    // and must request the load silently (no global blocking alert).
    const getSeasonsByFiltered = vi.fn().mockResolvedValue(undefined);
    mockedUseSeason.mockReturnValue({
      seasons: null,
      getSeasonsByFiltered,
    } as unknown as ReturnType<typeof useSeason>);

    renderPage();

    expect(
      await screen.findByText('No pudimos cargar las temporadas.')
    ).toBeInTheDocument();
    expect(getSeasonsByFiltered).toHaveBeenCalledWith(
      expect.objectContaining({ pageNumber: 1 }),
      { silent: true }
    );
  });

  it('re-runs the fetch when "Reintentar" is clicked', async () => {
    const getSeasonsByFiltered = vi.fn().mockResolvedValue(undefined);
    mockedUseSeason.mockReturnValue({
      seasons: null,
      getSeasonsByFiltered,
    } as unknown as ReturnType<typeof useSeason>);

    renderPage();

    const retry = await screen.findByRole('button', { name: 'Reintentar' });
    expect(getSeasonsByFiltered).toHaveBeenCalledTimes(1);

    fireEvent.click(retry);

    await waitFor(() =>
      expect(getSeasonsByFiltered).toHaveBeenCalledTimes(2)
    );
  });
});
