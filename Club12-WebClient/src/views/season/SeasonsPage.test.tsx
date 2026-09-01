import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import SeasonsPage from '@/views/season/SeasonsPage';
import { useSeason } from '@/modules/season/hook/season.hook';
import type { ISeasonResponse } from '@/modules/season/type/season';
import type { GUID } from '@/modules/core/types/types';

vi.mock('@/modules/season/hook/season.hook');

const mockedUseSeason = vi.mocked(useSeason);

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
});
