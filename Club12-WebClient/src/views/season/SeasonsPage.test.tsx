import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';
import SeasonsPage from '@/views/season/SeasonsPage';
import { useSeason } from '@/modules/season/hook/season.hook';
import type { ISeasonResponse } from '@/modules/season/type/season';
import type { GUID } from '@/modules/core/types/types';

vi.mock('@/modules/season/hook/season.hook');
vi.mock('sweetalert2', () => ({ default: { fire: vi.fn() } }));

import Swal from 'sweetalert2';

const mockedUseSeason = vi.mocked(useSeason);
const mockedSwalFire = vi.mocked(Swal.fire);

afterEach(() => {
  vi.clearAllMocks();
});

const buildSeason = (
  overrides: Partial<ISeasonResponse> = {}
): ISeasonResponse => ({
  id: '55555555-5555-5555-5555-555555555555' as GUID,
  slug: 'temporada-2026',
  name: 'Temporada 2026',
  year: 2026,
  tournaments: [],
  ...overrides,
});

describe('SeasonsPage — admin CRUD listing', () => {
  it('renders the loaded seasons in the grid', async () => {
    mockedUseSeason.mockReturnValue({
      seasons: [buildSeason()],
      addSeason: vi.fn(),
      putSeasonById: vi.fn(),
      deleteSeasonById: vi.fn(),
      getSeasonsByFiltered: vi.fn().mockResolvedValue(undefined),
    } as unknown as ReturnType<typeof useSeason>);

    render(
      <MemoryRouter>
        <SeasonsPage />
      </MemoryRouter>
    );

    expect(await screen.findByText('Temporada 2026')).toBeInTheDocument();
  });

  it('does not offer an Editar row action — editing lives inside the season detail page', async () => {
    mockedUseSeason.mockReturnValue({
      seasons: [buildSeason()],
      addSeason: vi.fn(),
      putSeasonById: vi.fn(),
      deleteSeasonById: vi.fn(),
      getSeasonsByFiltered: vi.fn().mockResolvedValue(undefined),
    } as unknown as ReturnType<typeof useSeason>);

    render(
      <MemoryRouter>
        <SeasonsPage />
      </MemoryRouter>
    );

    await screen.findByText('Temporada 2026');
    expect(screen.queryByTestId('EditIcon')).not.toBeInTheDocument();
  });

  it('does not show a success dialog or refetch when deleteSeasonById fails', async () => {
    const getSeasonsByFiltered = vi.fn().mockResolvedValue(undefined);
    const deleteSeasonById = vi.fn().mockResolvedValue(false);
    mockedUseSeason.mockReturnValue({
      seasons: [buildSeason()],
      addSeason: vi.fn(),
      putSeasonById: vi.fn(),
      deleteSeasonById,
      getSeasonsByFiltered,
    } as unknown as ReturnType<typeof useSeason>);
    mockedSwalFire.mockResolvedValue({
      isConfirmed: true,
      isDenied: false,
      isDismissed: false,
    } as Awaited<ReturnType<typeof Swal.fire>>);

    render(
      <MemoryRouter>
        <SeasonsPage />
      </MemoryRouter>
    );

    await screen.findByText('Temporada 2026');
    getSeasonsByFiltered.mockClear();

    const deleteIcon = await screen.findByTestId('DeleteIcon');
    (deleteIcon.closest('button') as HTMLButtonElement).click();

    await waitFor(() => expect(deleteSeasonById).toHaveBeenCalledTimes(1));

    expect(mockedSwalFire).not.toHaveBeenCalledWith(
      expect.objectContaining({ title: '¡Eliminada!' })
    );
    expect(getSeasonsByFiltered).not.toHaveBeenCalled();
  });
});
