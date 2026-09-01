import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import AdminSeasonDetailPage from '@/views/season/AdminSeasonDetailPage';
import { useSeason } from '@/modules/season/hook/season.hook';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import type { ISeasonResponse } from '@/modules/season/type/season';
import { TournamentCategory } from '@/modules/core/enum/tournament/tournamentCategory';
import type { GUID } from '@/modules/core/types/types';

vi.mock('@/modules/season/hook/season.hook');
vi.mock('sweetalert2', () => ({ default: { fire: vi.fn() } }));

const mockNavigate = vi.fn();
vi.mock('react-router-dom', async importOriginal => {
  const actual = await importOriginal<typeof import('react-router-dom')>();
  return { ...actual, useNavigate: () => mockNavigate };
});

const mockedUseSeason = vi.mocked(useSeason);

const SEASON_ID = '55555555-5555-5555-5555-555555555555' as GUID;

const buildSeason = (
  tournaments: ISeasonResponse['tournaments'] = [
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
  ]
): ISeasonResponse => ({
  id: SEASON_ID,
  slug: 'temporada-2026',
  name: 'Temporada 2026',
  year: 2026,
  tournaments,
});

const renderAt = (path: string) =>
  render(
    <MemoryRouter initialEntries={[path]}>
      <Routes>
        <Route
          path="/panel/temporadas/:seasonId"
          element={<AdminSeasonDetailPage />}
        />
      </Routes>
    </MemoryRouter>
  );

describe('AdminSeasonDetailPage', () => {
  it('renders the season name and groups tournaments by category', async () => {
    const getSeasonById = vi.fn().mockResolvedValue(buildSeason());
    mockedUseSeason.mockReturnValue({
      getSeasonById,
    } as unknown as ReturnType<typeof useSeason>);

    renderAt('/panel/temporadas/temporada-2026');

    expect(await screen.findByText('Temporada 2026')).toBeInTheDocument();
    expect(screen.getByText('Masculino')).toBeInTheDocument();
    expect(screen.getByText('Femenino')).toBeInTheDocument();
    expect(screen.getByText('Apertura Masculino')).toBeInTheDocument();
    expect(screen.getByText('Apertura Femenino')).toBeInTheDocument();
    expect(getSeasonById).toHaveBeenCalledWith('temporada-2026');

    // Cards link to the ADMIN tournament page (by slug), not the public one.
    const link = screen
      .getByText('Apertura Masculino')
      .closest('a') as HTMLAnchorElement;
    expect(link).toHaveAttribute(
      'href',
      APP_ROUTES.panelTournamentDetail.build('apertura-masculino')
    );
  });

  it('navigates to the wizard pre-scoped with the season id', async () => {
    const getSeasonById = vi.fn().mockResolvedValue(buildSeason());
    mockedUseSeason.mockReturnValue({
      getSeasonById,
    } as unknown as ReturnType<typeof useSeason>);

    renderAt('/panel/temporadas/temporada-2026');

    const button = await screen.findByRole('button', { name: /nuevo torneo/i });
    await userEvent.click(button);

    // The seasonId passed is the resolved GUID (not the URL slug), so the
    // wizard preselects the right season regardless of how it was reached.
    expect(mockNavigate).toHaveBeenCalledWith(
      APP_ROUTES.panelTournamentWizard,
      { state: { seasonId: SEASON_ID } }
    );
  });

  it('shows an empty state with the CTA when the season has no tournaments', async () => {
    const getSeasonById = vi.fn().mockResolvedValue(buildSeason([]));
    mockedUseSeason.mockReturnValue({
      getSeasonById,
    } as unknown as ReturnType<typeof useSeason>);

    renderAt('/panel/temporadas/temporada-2026');

    expect(
      await screen.findByText(/todavía no tiene torneos/i)
    ).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /nuevo torneo/i })
    ).toBeInTheDocument();
  });

  it('opens the edit dialog prefilled with the season\'s current values', async () => {
    const getSeasonById = vi.fn().mockResolvedValue(buildSeason());
    mockedUseSeason.mockReturnValue({
      getSeasonById,
      putSeasonById: vi.fn(),
    } as unknown as ReturnType<typeof useSeason>);

    const user = userEvent.setup();
    renderAt('/panel/temporadas/temporada-2026');

    await user.click(
      await screen.findByRole('button', { name: 'Editar temporada' })
    );

    const dialog = screen.getByRole('dialog');
    expect(within(dialog).getByRole('textbox', { name: /^Nombre/ })).toHaveValue(
      'Temporada 2026'
    );
    expect(within(dialog).getByRole('spinbutton', { name: /^Año/ })).toHaveValue(
      2026
    );
  });

  it('saves via putSeasonById, closes the dialog and refreshes the season', async () => {
    const getSeasonById = vi.fn().mockResolvedValue(buildSeason());
    const putSeasonById = vi.fn().mockResolvedValue(buildSeason());
    mockedUseSeason.mockReturnValue({
      getSeasonById,
      putSeasonById,
    } as unknown as ReturnType<typeof useSeason>);

    const user = userEvent.setup();
    renderAt('/panel/temporadas/temporada-2026');

    await user.click(
      await screen.findByRole('button', { name: 'Editar temporada' })
    );

    const dialog = screen.getByRole('dialog');
    const nameInput = within(dialog).getByRole('textbox', { name: /^Nombre/ });
    await user.clear(nameInput);
    await user.type(nameInput, 'Temporada 2027');

    getSeasonById.mockClear();
    await user.click(within(dialog).getByRole('button', { name: /guardar/i }));

    await waitFor(() => expect(putSeasonById).toHaveBeenCalledTimes(1));
    const [id, payload] = putSeasonById.mock.calls[0];
    expect(id).toBe(SEASON_ID);
    expect(payload).toEqual(expect.objectContaining({ name: 'Temporada 2027' }));

    await waitFor(() =>
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    );
    await waitFor(() => expect(getSeasonById).toHaveBeenCalledTimes(1));
  });
});
