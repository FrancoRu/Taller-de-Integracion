import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import PublicSeasonPage from '@/views/home/seasons/PublicSeasonPage';
import { useSeason } from '@/modules/season/hook/season.hook';
import type { ISeasonResponse } from '@/modules/season/type/season';
import { TournamentCategory } from '@/modules/core/enum/tournament/tournamentCategory';
import type { GUID } from '@/modules/core/types/types';

vi.mock('@/modules/season/hook/season.hook');

const mockedUseSeason = vi.mocked(useSeason);

const buildSeason = (): ISeasonResponse => ({
  id: '55555555-5555-5555-5555-555555555555' as GUID,
  slug: 'temporada-2026',
  name: 'Temporada 2026',
  year: 2026,
  tournaments: [
    {
      id: '11111111-1111-1111-1111-111111111111' as GUID,
      name: 'Apertura Masculino',
      slug: 'apertura-masculino',
      category: TournamentCategory.Masculine,
    },
    {
      id: '22222222-2222-2222-2222-222222222222' as GUID,
      name: 'Apertura Femenino',
      slug: 'apertura-femenino',
      category: TournamentCategory.Feminine,
    },
  ],
});

const renderAt = (path: string) =>
  render(
    <MemoryRouter initialEntries={[path]}>
      <Routes>
        <Route path="/temporadas/:seasonId" element={<PublicSeasonPage />} />
      </Routes>
    </MemoryRouter>
  );

describe('PublicSeasonPage — groups tournaments by category', () => {
  it('renders a Masculino and a Femenino section with each tournament', async () => {
    const getSeasonById = vi.fn().mockResolvedValue(buildSeason());
    mockedUseSeason.mockReturnValue({
      getSeasonById,
    } as unknown as ReturnType<typeof useSeason>);

    renderAt('/temporadas/temporada-2026');

    expect(await screen.findByText('Masculino')).toBeInTheDocument();
    expect(screen.getByText('Femenino')).toBeInTheDocument();
    expect(screen.getByText('Apertura Masculino')).toBeInTheDocument();
    expect(screen.getByText('Apertura Femenino')).toBeInTheDocument();
    expect(getSeasonById).toHaveBeenCalledWith('temporada-2026');
  });
});
